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

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxInputs(
    ITaxLedgerRepository ledger, IAssessmentRepository assessments, IBrokerStatementRepository statements, IFxRates fxRates, IInstrumentCatalog instruments,
    IAccountSetupQueries accounts, IJournalQueries journal)
{
    private const string Pln = "PLN";
    private const string YahooPrefix = "yfinance:";

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
                entry.Restate(current);

            if (entry is { IsReversed: false } && reversed.Contains(entry.TransactionId))
                entry.Reverse();
        }

        await ledger.SaveChangesAsync(cancellationToken);

        return account;
    }

    public async Task<UserId?> OwnerOfAsync(Guid accountId, CancellationToken cancellationToken) =>
        await accounts.GetAccountSetup(accountId, cancellationToken) is { } setup ? UserId.Of(setup.OwnerId) : null;

    public async Task<Taxpayer> TaxpayerAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var setups = (await accounts.GetAccountSetups(cancellationToken)).Where(setup => setup.OwnerId == ownerId.Value).OrderBy(setup => setup.Name).ToList();
        var known = new List<TaxAccount>();

        foreach (var setup in setups)
            if (await EnsureAccountAsync(setup.Id, cancellationToken) is { } account)
                known.Add(account);
        return new Taxpayer(ownerId, known, setups.ToDictionary(setup => setup.Id));
    }

    public async Task<(IReadOnlyList<TaxEvent> Events, IReadOnlyList<string> Missing)> EventsAsync(IReadOnlyList<TaxAccount> taxAccounts, CancellationToken cancellationToken)
    {
        var events = new List<TaxEvent>();
        var missing = new List<string>();

        foreach (var account in taxAccounts)
        {
            var (own, lacking) = await EventsAsync(account, cancellationToken);
            events.AddRange(own);
            missing.AddRange(lacking);
        }

        return (events, missing);
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
                entry.Allocations, rate.Value, date, Currency: entry.Currency, AccountId: account.AccountId));
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

    public async Task<IReadOnlyList<LossYear>> LossesAsync(UserId ownerId, CancellationToken cancellationToken) =>
        [.. (await assessments.LossesAsync(ownerId, cancellationToken)).Select(loss => loss.ToLossYear())];

    public static string HashFor(int year, IReadOnlyList<TaxEvent> events, IReadOnlyList<LossYear> losses, ITaxPolicy policy)
    {
        var input = JsonSerializer.Serialize(new
        {
            policy = $"{policy.Code}/{policy.Version}",
            events = events.Where(e => e.OccurredOn.Year <= year)
                .Select(e => new { e.TransactionId, e.AccountId, e.OccurredOn, e.Kind, e.Quantity, e.Price, e.Fee, e.Amount, e.FxRate, e.Allocations, e.Broker, e.Withholding, e.Currency }),
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
