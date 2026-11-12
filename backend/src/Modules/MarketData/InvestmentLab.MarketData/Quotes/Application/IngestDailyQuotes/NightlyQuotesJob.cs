using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

public sealed class NightlyQuotesJob(IngestDailyQuotesHandler ingest, ILogger<NightlyQuotesJob> logger) : IScheduledJob
{
    public static IJobSchedule Schedule { get; } = DailySchedule.Weekdays(new TimeOnly(22, 30), "Europe/Warsaw");

    public static bool RunAtStartup => true;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var report = await ingest.Handle(cancellationToken);
        logger.LogInformation(
            "Nightly quotes: {Added} added, {Updated} updated for {Instruments} instruments; missing {Missing}",
            report.QuotesAdded, report.QuotesUpdated, report.Instruments, report.MissingSymbols);
    }
}
