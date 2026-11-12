using InvestmentLab.SharedKernel;
using InvestmentLab.MarketData.FxRates.Application.Shared;
using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed class IngestFxRatesHandler(IFxRateStore store, IFxRateProvider provider, TimeProvider clock) : IHandler
{
    public const int InitialHistoryYears = 2;
    public static readonly Currency[] Currencies = [Currency.EUR, Currency.USD];

    public async Task<FxIngestReport> Handle(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var added = new Dictionary<string, IReadOnlyList<FxRate>>();

        foreach (var currency in Currencies)
            added[currency.Code] = await AddNewRatesAsync(currency, today, cancellationToken);

        if (FirstEffectiveDateOf(added.Values) is { } first)
            store.Record(new FxRatesIngested(first, now));

        await store.SaveChangesAsync(cancellationToken);

        return new FxIngestReport(added.ToDictionary(pair => pair.Key, pair => pair.Value.Count));
    }

    private static DateOnly? FirstEffectiveDateOf(IEnumerable<IReadOnlyList<FxRate>> added) =>
        added.SelectMany(rates => rates).Min(rate => (DateOnly?)rate.EffectiveDate);

    private async Task<IReadOnlyList<FxRate>> AddNewRatesAsync(Currency currency, DateOnly today, CancellationToken cancellationToken)
    {
        var last = await store.LastEffectiveDateAsync(provider.Source, currency, PrimaryFxSource.Pivot, cancellationToken);
        var from = last?.AddDays(1) ?? today.AddYears(-InitialHistoryYears);
        if (from > today)
            return [];

        var fetched = await provider.FetchAsync(currency, from, today, cancellationToken);
        var newRates = fetched.Where(rate => rate.EffectiveDate >= from).ToList();

        foreach (var rate in newRates)
            store.Add(rate);

        return newRates;
    }
}
