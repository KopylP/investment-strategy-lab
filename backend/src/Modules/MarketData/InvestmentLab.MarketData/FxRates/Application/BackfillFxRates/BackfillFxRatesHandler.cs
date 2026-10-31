using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
using InvestmentLab.MarketData.FxRates.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.FxRates.Application.BackfillFxRates;

public sealed class BackfillFxRatesHandler(
    IFxRateStore store,
    IFxRateProvider provider,
    IngestFxRatesHandler ingest,
    TimeProvider clock,
    ILogger<BackfillFxRatesHandler> logger) : IFxRateBackfill
{
    public async Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken)
    {
        var from = since.AddDays(-FxRateResolver.MaxAgeDays);

        try
        {
            foreach (var currency in IngestFxRatesHandler.Currencies)
                await AddOlderRatesAsync(currency, from, cancellationToken);

            await store.SaveChangesAsync(cancellationToken);
            await ingest.Handle(cancellationToken);

            return true;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "NBP rates since {Since} could not be loaded", since);

            return false;
        }
    }

    private async Task AddOlderRatesAsync(Currency currency, DateOnly from, CancellationToken cancellationToken)
    {
        var first = await store.FirstEffectiveDateAsync(provider.Source, currency, Currency.PLN, cancellationToken);
        var until = first?.AddDays(-1) ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        if (from > until)
            return;

        foreach (var rate in await provider.FetchAsync(currency, from, until, cancellationToken))
            store.Add(rate);
    }
}
