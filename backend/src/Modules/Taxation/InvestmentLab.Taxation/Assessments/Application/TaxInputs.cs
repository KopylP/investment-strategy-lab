using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
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

/// <summary>
/// Turns the ledger copy into tax events with NBP rates (last table before the transaction day, §6.7), with what the
/// broker's tax reports add to them (tax withheld from dividends, lots the journal has no buy for).
/// </summary>
public sealed class TaxInputs(
    ITaxLedgerRepository ledger, IAssessmentRepository assessments, IBrokerStatementRepository statements, IFxRates fxRates, IInstrumentCatalog instruments,
    IAccountSetupQueries accounts, IJournalQueries journal)
{
    private const string Pln = "PLN";
    private const string YahooPrefix = "yfinance:";

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
            else if (entry is not null && LedgerEntries.From(recorded, entry.RecordedAt) is { } current)
                entry.Restate(current);   // copies made by an earlier version of the rules (e.g. in the account currency) catch up
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
                entry.Allocations, rate.Value, date, Currency: entry.Currency));
        }

        var uploaded = await statements.ForAccountAsync(account.AccountId, cancellationToken);
        if (uploaded.Count == 0)
            return (events, missing);
        var (lots, dividends) = BrokerStatement.Combine(uploaded);
        var match = BrokerFacts.Match(events, lots, dividends, await InstrumentKeysAsync(cancellationToken));
        var currencies = entries.ToDictionary(entry => entry.TransactionId, entry => entry.Currency);
        var sales = new Dictionary<Guid, SaleFacts>();
        foreach (var (transactionId, facts) in match.Sales)
            sales[transactionId] = await AtNbpRatesAsync(facts, currencies.GetValueOrDefault(transactionId), rates, cancellationToken);
        return ([.. events.Select(e => e with { Broker = sales.GetValueOrDefault(e.TransactionId), Withholding = match.Dividends.GetValueOrDefault(e.TransactionId) })], missing);
    }

    /// <summary>
    /// The broker's lots at the policy's rate (NBP, the day before each purchase) instead of the broker's own; its cost
    /// basis is comparable with ours only when the journal entry is in the same currency.
    /// </summary>
    private async Task<SaleFacts> AtNbpRatesAsync(SaleFacts facts, string? entryCurrency, Dictionary<(string, DateOnly), FxRateDto?> rates, CancellationToken cancellationToken)
    {
        var lots = new List<BrokerLotFact>();
        foreach (var lot in facts.Lots)
        {
            var (rate, _) = await RateAsync(facts.Currency, lot.AcquiredOn, rates, cancellationToken);
            lots.Add(rate is { } nbp ? lot with { CostPln = decimal.Round(lot.Cost * nbp, 2, MidpointRounding.AwayFromZero) } : lot);
        }
        return facts with { Lots = lots, Cost = entryCurrency == facts.Currency ? facts.Cost : null };
    }

    private async Task<IReadOnlyDictionary<Guid, InstrumentKey>> InstrumentKeysAsync(CancellationToken cancellationToken) =>
        (await instruments.GetInstruments(cancellationToken)).ToDictionary(
            instrument => instrument.Id,
            instrument => new InstrumentKey(instrument.Isin,
                [.. instrument.ProviderSymbols.Where(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal)).Select(symbol => symbol[YahooPrefix.Length..])]));

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
                .Select(e => new { e.TransactionId, e.OccurredOn, e.Kind, e.Quantity, e.Price, e.Fee, e.Amount, e.FxRate, e.Allocations, e.Broker, e.Withholding, e.Currency }),
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

/// <summary>
/// Journal entries that matter for tax: trades and dividends. One settled in another currency than the account's (a
/// USD stock on a EUR account) is taken in that currency, so it gets that currency's NBP rate of the day before — not
/// the account currency's rate applied to an amount the import converted at another day's cross rate.
/// </summary>
public static class LedgerEntries
{
    /// <param name="recordedAt">Keeps the journal order of entries of the same day.</param>
    public static TaxLedgerEntry? From(TransactionRecordedV1 recorded, DateTimeOffset recordedAt)
    {
        var owner = UserId.Of(recorded.OwnerId);
        if (recorded.Trade is { } trade)
        {
            var (price, fee, currency) = (trade.Price, trade.Fee, recorded.Currency);
            var total = trade.Side == "Buy" ? trade.Quantity * trade.Price + trade.Fee : trade.Quantity * trade.Price - trade.Fee;
            if (trade is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: 3 } own } && own != recorded.Currency && total > 0m)
            {
                // The settled total is quantity × price ± fee in the trade's own currency: scale price and fee back to it.
                var back = settled / total;
                (price, fee, currency) = (decimal.Round(trade.Price * back, 8), decimal.Round(trade.Fee * back, 2), own);
            }
            return TaxLedgerEntry.Trade(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                trade.Side == "Buy", trade.InstrumentId, trade.Quantity, price, fee, currency,
                [.. trade.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Quantity))]);
        }
        if (recorded.Cash is { Kind: "Dividend", InstrumentId: { } instrumentId } cash)
        {
            var (amount, currency) = cash is { SettledAmount: > 0m and var settled, SettledCurrency: { Length: 3 } own } && own != recorded.Currency
                ? (settled, own)
                : (cash.Amount, recorded.Currency);
            return TaxLedgerEntry.Dividend(recorded.TransactionId, recorded.AccountId, owner, recorded.OccurredOn, recordedAt,
                instrumentId, amount, currency, [.. cash.Allocations.Select(allocation => new EntryAllocation(allocation.SubPortfolioId, allocation.Amount))]);
        }
        return null;
    }
}
