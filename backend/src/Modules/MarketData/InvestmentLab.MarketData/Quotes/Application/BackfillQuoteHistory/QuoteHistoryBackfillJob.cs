using InvestmentLab.BuildingBlocks.Scheduling;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.MarketData.Quotes.Application.BackfillQuoteHistory;

public sealed class QuoteHistoryBackfillJob(BackfillQuoteHistoryHandler backfill, ILogger<QuoteHistoryBackfillJob> logger) : IScheduledJob
{
    public static readonly DailySchedule Schedule = DailySchedule.Weekdays(new TimeOnly(4, 0), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var added = await backfill.Handle(cancellationToken);

        logger.LogInformation("Quote history backfill: {Added} older quotes added", added);
    }
}
