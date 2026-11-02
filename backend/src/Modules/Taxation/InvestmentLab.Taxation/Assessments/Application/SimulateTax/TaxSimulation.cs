using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Contracts;
using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.SimulateTax;

public sealed class TaxSimulation(ITaxCurrencyRates currencyRates, IFxRateBackfill backfill) : ITaxSimulation
{
    public async Task<SimulatedTaxDto> SimulateAsync(IReadOnlyList<SimulatedTradeDto> trades, CancellationToken cancellationToken)
    {
        var policy = TaxPolicies.Default;
        if (!policy.IsTaxed || trades.Count == 0)
            return new SimulatedTaxDto(policy.Code, policy.FxRule.TaxCurrency, []);

        await EnsureRatesSinceAsync(trades, cancellationToken);
        var events = await EventsAsync(trades, policy.FxRule, cancellationToken);

        var computation = TaxCalculator.Compute(events, policy, []);
        var years = computation.Years.Select(year => new SimulatedTaxYearDto(year.Summary.Year, year.Summary.TaxDue));

        return new SimulatedTaxDto(policy.Code, policy.FxRule.TaxCurrency, [.. years]);
    }

    private async Task EnsureRatesSinceAsync(IReadOnlyList<SimulatedTradeDto> trades, CancellationToken cancellationToken)
    {
        var earliest = trades.Min(trade => trade.On);

        if (!await backfill.EnsureSinceAsync(earliest, cancellationToken))
            throw new InvalidOperationException($"Exchange rates since {earliest} could not be loaded for a tax simulation.");
    }

    private async Task<List<TaxEvent>> EventsAsync(IReadOnlyList<SimulatedTradeDto> trades, IFxRule fxRule, CancellationToken cancellationToken)
    {
        var rates = new Dictionary<(string, DateOnly), decimal>();
        var events = new List<TaxEvent>(trades.Count);

        for (var sequence = 0; sequence < trades.Count; sequence++)
        {
            var trade = trades[sequence];
            var rate = await RateAsync(trade.Currency, fxRule.LookupDate(trade.On), fxRule.TaxCurrency, rates, cancellationToken);

            events.Add(EventOf(trade, sequence, rate));
        }

        return events;
    }

    private async Task<decimal> RateAsync(
        string currency, DateOnly lookupDate, string taxCurrency, Dictionary<(string, DateOnly), decimal> cache, CancellationToken cancellationToken)
    {
        if (string.Equals(currency, taxCurrency, StringComparison.OrdinalIgnoreCase))
            return 1m;

        if (cache.TryGetValue((currency, lookupDate), out var known))
            return known;

        var found = await currencyRates.OnAsync(currency, taxCurrency, lookupDate, cancellationToken)
            ?? throw new InvalidOperationException($"No {currency}/{taxCurrency} rate for {lookupDate} in a tax simulation.");

        return cache[(currency, lookupDate)] = found.Rate;
    }

    private static TaxEvent EventOf(SimulatedTradeDto trade, int sequence, decimal rate) => new(
        TransactionId: Guid.NewGuid(),
        OccurredOn: trade.On,
        Sequence: sequence,
        Kind: trade.IsSale ? TaxableKind.Sell : TaxableKind.Buy,
        InstrumentId: trade.InstrumentId,
        Quantity: trade.Quantity,
        Price: trade.Price,
        Fee: trade.Fee,
        Amount: 0m,
        Allocations: [],
        FxRate: rate,
        FxRateDate: null,
        Currency: trade.Currency);
}
