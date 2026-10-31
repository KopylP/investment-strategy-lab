using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed class IngestFxRatesHandler(IFxRateStore store, IFxRateProvider provider, TimeProvider clock, ILogger<IngestFxRatesHandler> logger) : IFxRateBackfill
{
    public const int InitialHistoryYears = 2;
    public static readonly Currency[] Currencies = [Currency.EUR, Currency.USD];

    public async Task<FxIngestReport> Handle(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var added = new Dictionary<string, int>();

        foreach (var currency in Currencies)
        {
            var last = await store.LastEffectiveDateAsync(provider.Source, currency, Currency.PLN, cancellationToken);
            var from = last?.AddDays(1) ?? today.AddYears(-InitialHistoryYears);
            var rates = from <= today ? await provider.FetchAsync(currency, from, today, cancellationToken) : [];
            foreach (var rate in rates.Where(rate => rate.EffectiveDate >= from))
                store.Add(rate);

            added[currency.Code] = rates.Count(rate => rate.EffectiveDate >= from);
        }

        await store.SaveChangesAsync(cancellationToken);

        return new FxIngestReport(added);
    }

    public async Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken)
    {
        var from = since.AddDays(-FxRateResolver.MaxAgeDays);

        try
        {
            foreach (var currency in Currencies)
            {
                var first = await store.FirstEffectiveDateAsync(provider.Source, currency, Currency.PLN, cancellationToken);
                var until = first?.AddDays(-1) ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
                if (from <= until)
                    foreach (var rate in await provider.FetchAsync(currency, from, until, cancellationToken))
                        store.Add(rate);
            }

            await store.SaveChangesAsync(cancellationToken);
            await Handle(cancellationToken);

            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "NBP rates since {Since} could not be loaded", since);

            return false;
        }
    }
}
