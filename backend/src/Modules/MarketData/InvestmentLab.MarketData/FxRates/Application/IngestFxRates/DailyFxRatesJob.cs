using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.FxRates.Application.IngestFxRates;

public sealed class DailyFxRatesJob(IngestFxRatesHandler ingest, ILogger<DailyFxRatesJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = DailySchedule.Weekdays(new TimeOnly(13, 0), "Europe/Warsaw");

    public static bool RunAtStartup => true;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var report = await ingest.Handle(cancellationToken);
        logger.LogInformation("NBP rates added: {Added}", report.RatesAdded);
    }
}
