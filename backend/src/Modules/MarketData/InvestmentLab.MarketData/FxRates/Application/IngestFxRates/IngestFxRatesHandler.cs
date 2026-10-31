using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed class IngestFxRatesHandler(IFxRateStore store, IFxRateProvider provider, TimeProvider clock)
{
    public const int InitialHistoryYears = 2;
    public static readonly Currency[] Currencies = [Currency.EUR, Currency.USD];

    public async Task<FxIngestReport> Handle(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var added = new Dictionary<string, int>();

        foreach (var currency in Currencies)
            added[currency.Code] = await AddNewRatesAsync(currency, today, cancellationToken);

        await store.SaveChangesAsync(cancellationToken);

        return new FxIngestReport(added);
    }

    private async Task<int> AddNewRatesAsync(Currency currency, DateOnly today, CancellationToken cancellationToken)
    {
        var last = await store.LastEffectiveDateAsync(provider.Source, currency, Currency.PLN, cancellationToken);
        var from = last?.AddDays(1) ?? today.AddYears(-InitialHistoryYears);
        if (from > today)
            return 0;

        var fetched = await provider.FetchAsync(currency, from, today, cancellationToken);
        var newRates = fetched.Where(rate => rate.EffectiveDate >= from).ToList();

        foreach (var rate in newRates)
            store.Add(rate);

        return newRates.Count;
    }
}
