using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.MarketData.FxRates.Domain;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed record FxIngestReport(IReadOnlyDictionary<string, int> RatesAdded);

/// <summary>Stores NBP rates of the trading currencies from the day after the newest stored one, or two years back.</summary>
public sealed class IngestFxRatesHandler(IFxRateStore store, IFxRateProvider provider, TimeProvider clock)
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
}

/// <summary>NBP publishes table A on business days around 12:00; 13:00 Warsaw leaves a margin.</summary>
public sealed class DailyFxRatesJob(IngestFxRatesHandler ingest, ILogger<DailyFxRatesJob> logger) : IScheduledJob
{
    public static readonly DailySchedule Schedule = DailySchedule.Weekdays(new TimeOnly(13, 0), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var report = await ingest.Handle(cancellationToken);
        logger.LogInformation("NBP rates added: {Added}", report.RatesAdded);
    }
}
