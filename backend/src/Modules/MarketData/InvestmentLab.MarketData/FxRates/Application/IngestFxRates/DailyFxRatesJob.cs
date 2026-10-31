using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed class DailyFxRatesJob(IngestFxRatesHandler ingest, ILogger<DailyFxRatesJob> logger) : IScheduledJob
{
    public static readonly DailySchedule Schedule = DailySchedule.Weekdays(new TimeOnly(13, 0), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var report = await ingest.Handle(cancellationToken);
        logger.LogInformation("NBP rates added: {Added}", report.RatesAdded);
    }
}
