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
    ITaxLedgerRepository ledger,
    IAssessmentRepository assessments,
    IBrokerStatementRepository statements,
    IFxRates fxRates,
    IInstrumentCatalog instruments,

    IAccountSetupQueries accounts, IJournalQueries journal)
{
    private const string Pln = "PLN";
    private const string YahooPrefix = "yfinance:";

    public async Task<TaxAccount?> EnsureAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var setup = await accounts.GetAccountSetup(accountId, cancellationToken);
        if (setup is null || await journal.GetJournal(accountId, cancellationToken) is not { } snapshot)
            return null;

        var account = await SynchronizedAccountAsync(setup, cancellationToken);
        await CopyJournalAsync(accountId, snapshot, cancellationToken);
        await ledger.SaveChangesAsync(cancellationToken);

        return account;
    }

    private async Task<TaxAccount> SynchronizedAccountAsync(AccountSetupDto setup, CancellationToken cancellationToken)
    {
        var account = await ledger.FindAccountAsync(setup.Id, cancellationToken);

        if (account is not null)
        {
            account.Update(setup.BaseCurrency, setup.TaxPolicyCode);

            return account;
        }

        var added = TaxAccount.Of(setup.Id, UserId.Of(setup.OwnerId), setup.BaseCurrency, setup.TaxPolicyCode);
        ledger.AddAccount(added);

        return added;
    }

    private async Task CopyJournalAsync(Guid accountId, JournalSnapshotDto snapshot, CancellationToken cancellationToken)
    {
        var copied = (await ledger.EntriesAsync(accountId, cancellationToken)).ToDictionary(entry => entry.TransactionId);
        var reversed = snapshot.ReversedIds.ToHashSet();
        var recordedAt = DateTimeOffset.UnixEpoch;

        foreach (var recorded in snapshot.Entries)
        {
            recordedAt = recordedAt.AddSeconds(1);

            var entry = CopyEntry(recorded, copied.GetValueOrDefault(recorded.TransactionId), recordedAt);

            if (entry is { IsReversed: false } && reversed.Contains(entry.TransactionId))
                entry.Reverse();
        }
    }

    private TaxLedgerEntry? CopyEntry(TransactionRecordedV1 recorded, TaxLedgerEntry? existing, DateTimeOffset recordedAt)
    {
        if (existing is null)
        {
            var added = LedgerEntries.From(recorded, recordedAt);

            if (added is not null)
                ledger.AddEntry(added);

            return added;
        }

        if (LedgerEntries.From(recorded, existing.RecordedAt) is { } current)
            existing.Restate(current);

        return existing;
    }

    public async Task<UserId?> OwnerOfAsync(Guid accountId, CancellationToken cancellationToken) =>
        await accounts.GetAccountSetup(accountId, cancellationToken) is { } setup ? UserId.Of(setup.OwnerId) : null;

    public async Task<Taxpayer> TaxpayerAsync(UserId ownerId, CancellationToken cancellationToken)
    {
        var all = await accounts.GetAccountSetups(cancellationToken);
        var setups = all.Where(setup => setup.OwnerId == ownerId.Value).OrderBy(setup => setup.Name).ToList();
        var known = new List<TaxAccount>();

        foreach (var setup in setups)
        {
            if (await EnsureAccountAsync(setup.Id, cancellationToken) is { } account)
                known.Add(account);
        }

        return new Taxpayer(ownerId, known, setups.ToDictionary(setup => setup.Id));
    }

    public async Task<(IReadOnlyList<TaxEvent> Events, IReadOnlyList<string> Missing)> EventsAsync(
        IReadOnlyList<TaxAccount> taxAccounts,
        CancellationToken cancellationToken)
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

    public async Task<(IReadOnlyList<TaxEvent> Events, IReadOnlyList<string> Missing)> EventsAsync(
        TaxAccount account,
        CancellationToken cancellationToken)
    {
        var all = await ledger.EntriesAsync(account.AccountId, cancellationToken);
        var entries = all.Where(entry => !entry.IsReversed).OrderBy(entry => entry.OccurredOn).ThenBy(entry => entry.RecordedAt).ToList();
        var rates = new Dictionary<(string, DateOnly), FxRateDto?>();

        var (events, missing) = await PricedEventsAsync(account, entries, rates, cancellationToken);

        var uploaded = await statements.ForAccountAsync(account.AccountId, cancellationToken);
        if (uploaded.Count == 0)
            return (events, missing);

        var withFacts = await WithBrokerFactsAsync(events, entries, uploaded, rates, cancellationToken);

        return (withFacts, missing);
    }

    private async Task<(List<TaxEvent> Events, List<string> Missing)> PricedEventsAsync(
        TaxAccount account,
        List<TaxLedgerEntry> entries,
        Dictionary<(string, DateOnly), FxRateDto?> rates,
        CancellationToken cancellationToken)
    {
        var events = new List<TaxEvent>();
        var missing = new List<string>();

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var (rate, date) = await RateAsync(entry.Currency, entry.OccurredOn, rates, cancellationToken);

            if (rate is { } known)
                events.Add(EventOf(entry, index, known, date, account.AccountId));
            else
                missing.Add($"{entry.Currency} {entry.OccurredOn:yyyy-MM-dd}");
        }

        return (events, missing);
    }

    private static TaxEvent EventOf(TaxLedgerEntry entry, int sequence, decimal rate, DateOnly? rateDate, Guid accountId) => new(
        entry.TransactionId,
        entry.OccurredOn,
        sequence,
        entry.Kind,
        entry.InstrumentId,
        entry.Quantity,
        entry.Price,
        entry.Fee,
        entry.Amount,
        entry.Allocations,
        rate,
        rateDate,
        Currency: entry.Currency,
        AccountId: accountId);

    private async Task<List<TaxEvent>> WithBrokerFactsAsync(
        List<TaxEvent> events,
        List<TaxLedgerEntry> entries,
        IReadOnlyList<BrokerStatement> uploaded,
        Dictionary<(string, DateOnly), FxRateDto?> rates,
        CancellationToken cancellationToken)
    {
        var (lots, dividends) = BrokerStatement.Combine(uploaded);
        var match = BrokerFacts.Match(events, lots, dividends, await InstrumentKeysAsync(cancellationToken));
        var currencies = entries.ToDictionary(entry => entry.TransactionId, entry => entry.Currency);
        var sales = new Dictionary<Guid, SaleFacts>();

        foreach (var (transactionId, facts) in match.Sales)
            sales[transactionId] = await AtNbpRatesAsync(facts, currencies.GetValueOrDefault(transactionId), rates, cancellationToken);

        return [.. events.Select(each => each with
        {
            Broker = sales.GetValueOrDefault(each.TransactionId),
            Withholding = match.Dividends.GetValueOrDefault(each.TransactionId),
        })];
    }

    private async Task<SaleFacts> AtNbpRatesAsync(
        SaleFacts facts,
        string? entryCurrency,
        Dictionary<(string, DateOnly), FxRateDto?> rates,
        CancellationToken cancellationToken)
    {
        var lots = new List<BrokerLotFact>();

        foreach (var lot in facts.Lots)
        {
            var (rate, _) = await RateAsync(facts.Currency, lot.AcquiredOn, rates, cancellationToken);
            lots.Add(rate is { } nbp ? lot with { CostPln = decimal.Round(lot.Cost * nbp, 2, MidpointRounding.AwayFromZero) } : lot);
        }

        return facts with { Lots = lots, Cost = entryCurrency == facts.Currency ? facts.Cost : null };
    }

    private async Task<IReadOnlyDictionary<Guid, InstrumentKey>> InstrumentKeysAsync(CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);

        return known.ToDictionary(instrument => instrument.Id, InstrumentKeyOf);
    }

    private static InstrumentKey InstrumentKeyOf(InstrumentDto instrument)
    {
        var yahooSymbols = instrument.ProviderSymbols.Where(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal));

        return new InstrumentKey(instrument.Isin, [.. yahooSymbols.Select(symbol => symbol[YahooPrefix.Length..])]);
    }

    public async Task<(decimal? Rate, DateOnly? Date)> RateAsync(string currency, DateOnly transactionDate, CancellationToken cancellationToken) =>
        await RateAsync(currency, transactionDate, [], cancellationToken);

    public async Task<IReadOnlyList<LossYear>> LossesAsync(UserId ownerId, CancellationToken cancellationToken) =>
        [.. (await assessments.LossesAsync(ownerId, cancellationToken)).Select(loss => loss.ToLossYear())];

    public static string HashFor(int year, IReadOnlyList<TaxEvent> events, IReadOnlyList<LossYear> losses, ITaxPolicy policy)
    {
        var yearEvents = events.Where(each => each.OccurredOn.Year <= year).Select(each => new
        {
            each.TransactionId,
            each.AccountId,
            each.OccurredOn,
            each.Kind,
            each.Quantity,
            each.Price,
            each.Fee,
            each.Amount,
            each.FxRate,
            each.Allocations,
            each.Broker,
            each.Withholding,
            each.Currency,
        });

        var input = JsonSerializer.Serialize(new
        {
            policy = $"{policy.Code}/{policy.Version}",
            events = yearEvents,
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
