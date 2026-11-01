using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed class TaxEventReader(ITaxLedgerRepository ledger, IBrokerStatementRepository statements, ITaxRates taxRates, IInstrumentKeys instrumentKeys)
{
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
        var rates = new Dictionary<(string, DateOnly), ITaxRate?>();

        var fxRule = TaxPolicies.For(account.PolicyCode).FxRule;

        var (events, missing) = await PricedEventsAsync(account, entries, fxRule, rates, cancellationToken);

        var uploaded = await statements.ForAccountAsync(account.AccountId, cancellationToken);
        if (uploaded.Count == 0)
            return (events, missing);

        var withFacts = await WithBrokerFactsAsync(events, entries, uploaded, fxRule, rates, cancellationToken);

        return (withFacts, missing);
    }

    private async Task<(List<TaxEvent> Events, List<string> Missing)> PricedEventsAsync(
        TaxAccount account,
        List<TaxLedgerEntry> entries,
        IFxRule fxRule,
        Dictionary<(string, DateOnly), ITaxRate?> rates,
        CancellationToken cancellationToken)
    {
        var events = new List<TaxEvent>();
        var missing = new List<string>();

        for (var index = 0; index < entries.Count; index++)
        {
            var entry = entries[index];
            var (rate, date) = await RateAsync(entry.Currency, fxRule.LookupDate(entry.OccurredOn), fxRule, rates, cancellationToken);

            if (rate is { } known)
                events.Add(EventOf(entry, index, known, date, account.AccountId));
            else
                missing.Add($"{entry.Currency} {entry.OccurredOn:yyyy-MM-dd}");
        }

        return (events, missing);
    }

    private static TaxEvent EventOf(TaxLedgerEntry entry, int sequence, decimal rate, DateOnly? rateDate, Guid accountId) => new(
        TransactionId: entry.TransactionId,
        OccurredOn: entry.OccurredOn,
        Sequence: sequence,
        Kind: entry.Kind,
        InstrumentId: entry.InstrumentId,
        Quantity: entry.Quantity,
        Price: entry.Price,
        Fee: entry.Fee,
        Amount: entry.Amount,
        Allocations: entry.Allocations,
        FxRate: rate,
        FxRateDate: rateDate,
        Currency: entry.Currency,
        AccountId: accountId);

    private async Task<List<TaxEvent>> WithBrokerFactsAsync(
        List<TaxEvent> events,
        List<TaxLedgerEntry> entries,
        IReadOnlyList<BrokerStatement> uploaded,
        IFxRule fxRule,
        Dictionary<(string, DateOnly), ITaxRate?> rates,
        CancellationToken cancellationToken)
    {
        var (lots, dividends) = BrokerStatement.Combine(uploaded);
        var match = BrokerFacts.Match(events, lots, dividends, await instrumentKeys.AllAsync(cancellationToken));
        var currencies = entries.ToDictionary(entry => entry.TransactionId, entry => entry.Currency);
        var sales = new Dictionary<Guid, SaleFacts>();

        foreach (var (transactionId, facts) in match.Sales)
            sales[transactionId] = await AtTaxRatesAsync(facts, currencies.GetValueOrDefault(transactionId), fxRule, rates, cancellationToken);

        return [.. events.Select(each => each with
        {
            Broker = sales.GetValueOrDefault(each.TransactionId),
            Withholding = match.Dividends.GetValueOrDefault(each.TransactionId),
        })];
    }

    private async Task<SaleFacts> AtTaxRatesAsync(
        SaleFacts facts,
        string? entryCurrency,
        IFxRule fxRule,
        Dictionary<(string, DateOnly), ITaxRate?> rates,
        CancellationToken cancellationToken)
    {
        var lots = new List<BrokerLotFact>();

        foreach (var lot in facts.Lots)
        {
            var (rate, _) = await RateAsync(facts.Currency, fxRule.LookupDate(lot.AcquiredOn), fxRule, rates, cancellationToken);
            lots.Add(rate is { } known ? lot.AtTaxRate(known) : lot);
        }

        return facts.AtTaxRates(lots, entryCurrency);
    }

    public async Task<(decimal? Rate, DateOnly? Date)> LatestRateAsync(string currency, DateOnly asOf, IFxRule fxRule, CancellationToken cancellationToken) =>
        await RateAsync(currency, asOf.AddDays(1), fxRule, [], cancellationToken);

    private async Task<(decimal? Rate, DateOnly? Date)> RateAsync(
        string currency, DateOnly lookupDate, IFxRule fxRule, Dictionary<(string, DateOnly), ITaxRate?> cache, CancellationToken cancellationToken)
    {
        if (currency == fxRule.TaxCurrency)
            return (1m, null);

        if (!cache.TryGetValue((currency, lookupDate), out var rate))
            rate = cache[(currency, lookupDate)] = await taxRates.OnAsync(currency, fxRule.TaxCurrency, lookupDate, cancellationToken);

        return rate is null ? (null, null) : (rate.Rate, rate.EffectiveDate);
    }
}
