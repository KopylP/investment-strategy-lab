using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;

/// <summary>22:30 Warsaw: Xetra closed at 17:30, US markets at 22:00, so Yahoo has both closes.</summary>
public sealed class NightlyQuotesJob(IngestDailyQuotesHandler ingest, ILogger<NightlyQuotesJob> logger) : IScheduledJob
{
    public static readonly DailySchedule Schedule = DailySchedule.Weekdays(new TimeOnly(22, 30), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var report = await ingest.Handle(cancellationToken);
        logger.LogInformation(
            "Nightly quotes: {Added} added, {Updated} updated for {Instruments} instruments; missing {Missing}",
            report.QuotesAdded, report.QuotesUpdated, report.Instruments, report.MissingSymbols);
    }
}
