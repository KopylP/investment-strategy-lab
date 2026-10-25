using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public interface IAssessmentRepository
{
    Task<IReadOnlyList<TaxAssessment>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<LossCarryforward>> LossesAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(TaxAssessment assessment);

    void AddLoss(LossCarryforward loss);

    void RemoveLoss(LossCarryforward loss);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Turns the ledger copy into tax events with NBP rates (last table before the transaction day, §6.7).</summary>
public sealed class TaxInputs(ITaxLedgerRepository ledger, IAssessmentRepository assessments, IFxRates fxRates, IAccountSetupQueries accounts, IJournalQueries journal)
{
    private const string Pln = "PLN";

    /// <summary>
    /// The account's tax facts, with the ledger copy caught up from Portfolio's journal: entries the events have not
    /// brought yet are added, reversals applied. Idempotent, so reads never show a stale year.
    /// </summary>
    public async Task<TaxAccount?> EnsureAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.GetAccountSetup(accountId, cancellationToken);
        if (setup is null || await journal.GetJournal(accountId, cancellationToken) is not { } snapshot)
            return null;

        var account = await ledger.FindAccountAsync(accountId, cancellationToken);
        if (account is null)
        {
            account = TaxAccount.Of(accountId, UserId.Of(setup.OwnerId), setup.BaseCurrency, setup.TaxPolicyCode);
            ledger.AddAccount(account);
        }
        else
        {
            account.Update(setup.BaseCurrency, setup.TaxPolicyCode);
        }

        var copied = (await ledger.EntriesAsync(accountId, cancellationToken)).ToDictionary(entry => entry.TransactionId);
        var reversed = snapshot.ReversedIds.ToHashSet();
        var recordedAt = DateTimeOffset.UnixEpoch;
        foreach (var recorded in snapshot.Entries)
        {
            recordedAt = recordedAt.AddSeconds(1);
            if (!copied.TryGetValue(recorded.TransactionId, out var entry) && LedgerEntries.From(recorded, recordedAt) is { } added)
            {
                ledger.AddEntry(added);
                entry = added;
            }
            if (entry is { IsReversed: false } && reversed.Contains(entry.TransactionId))
                entry.Reverse();
        }

        await ledger.SaveChangesAsync(cancellationToken);
        return account;
    }

    public async Task<(IReadOnlyList<TaxEvent> Events, IReadOnlyList<string> Missing)> EventsAsync(TaxAccount account, CancellationToken cancellationToken)
    {
        var entries = (await ledger.EntriesAsync(account.AccountId, cancellationToken)).Where(entry => !entry.IsReversed)
            .OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.RecordedAt).ToList();
        var rates = new Dictionary<(string, DateOnly), FxRateDto?>();
        var events = new List<TaxEvent>();
        var missing = new List<string>();
        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var (rate, date) = await RateAsync(entry.Currency, entry.OccurredOn, rates, cancellationToken);
            if (rate is null)
            {
                missing.Add($"{entry.Currency} {entry.OccurredOn:yyyy-MM-dd}");
                continue;
            }
            events.Add(new TaxEvent(entry.TransactionId, entry.OccurredOn, index, entry.Kind, entry.InstrumentId, entry.Quantity, entry.Price, entry.Fee, entry.Amount,
                entry.Allocations, rate.Value, date));
        }
        return (events, missing);
    }

    public async Task<(decimal? Rate, DateOnly? Date)> RateAsync(string currency, DateOnly transactionDate, CancellationToken cancellationToken) =>
        await RateAsync(currency, transactionDate, [], cancellationToken);

    public async Task<IReadOnlyList<LossYear>> LossesAsync(Guid accountId, CancellationToken cancellationToken) =>
        [.. (await assessments.LossesAsync(accountId, cancellationToken)).Select(loss => loss.ToLossYear())];

    /// <summary>Fingerprint of everything a year depends on: events up to its end, rates, carried losses, the policy.</summary>
    public static string HashFor(int year, IReadOnlyList<TaxEvent> events, IReadOnlyList<LossYear> losses, ITaxPolicy policy)
    {
        var input = JsonSerializer.Serialize(new
        {
            policy = $"{policy.Code}/{policy.Version}",
            events = events.Where(e => e.OccurredOn.Year <= year)
                .Select(e => new { e.TransactionId, e.OccurredOn, e.Kind, e.Quantity, e.Price, e.Fee, e.Amount, e.FxRate, e.Allocations }),
            losses,
        });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input)));
    }

    private async Task<(decimal? Rate, DateOnly? Date)> RateAsync(
        string currency, DateOnly transactionDate, Dictionary<(string, DateOnly), FxRateDto?> cache, CancellationToken cancellationToken)
    {
        if (currency == Pln)
            return (1m, null);
        if (!cache.TryGetValue((currency, transactionDate), out var rate))
            rate = cache[(currency, transactionDate)] = await fxRates.GetFxRate(currency, Pln, transactionDate, cancellationToken);
        return rate is null ? (null, null) : (rate.Rate, rate.EffectiveDate);
    }
}

/// <summary>Journal entries that matter for tax: trades and dividends.</summary>
public static class LedgerEntries
{
    /// <param name="recordedAt">Keeps the journal order of entries of the same day.</param>
    public static TaxLedgerEntry? From(TransactionRecordedV1 recorded, DateTimeOffset recordedAt)
    {
        var owner = UserId.Of(recorded.OwnerId);
        if (recorded.Trade is { } trade)
            return TaxLedgerEntry.Trade(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                trade.Side == "Buy", trade.InstrumentId, trade.Quantity, trade.Price, trade.Fee, recorded.Currency,
                [.. trade.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Quantity))]);
        if (recorded.Cash is { Kind: "Dividend", InstrumentId: { } instrumentId } cash)
            return TaxLedgerEntry.Dividend(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                instrumentId, cash.Amount, recorded.Currency, [.. cash.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Amount))]);
        return null;
    }
}
