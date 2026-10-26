using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

/// <summary>
/// Marks snapshots outdated from the day a change reaches back to (D9), keeps copies of plans.
/// Rebuilding happens on the next read or in the nightly job.
/// </summary>
internal sealed class PerformanceEventHandlers(IPerformanceRepository store)
    : IIntegrationEventHandler<TransactionRecordedV1>,
        IIntegrationEventHandler<TransactionReversedV1>,
        IIntegrationEventHandler<DailyQuotesIngestedV1>,
        IIntegrationEventHandler<QuoteCorrectedV1>,
        IIntegrationEventHandler<ReportGeneratedV1>,
        IIntegrationEventHandler<ReportSupersededV1>
{
    public Task Handle(TransactionRecordedV1 recorded, CancellationToken cancellationToken) =>
        MarkDirtyAsync(recorded.AccountId, recorded.OwnerId, recorded.OccurredOn, cancellationToken);

    public Task Handle(TransactionReversedV1 reversed, CancellationToken cancellationToken) =>
        MarkDirtyAsync(reversed.AccountId, reversed.OwnerId, reversed.OriginalOccurredOn, cancellationToken);

    public Task Handle(DailyQuotesIngestedV1 ingested, CancellationToken cancellationToken) => MarkAllDirtyAsync(ingested.FirstTradingDate, cancellationToken);

    public Task Handle(QuoteCorrectedV1 corrected, CancellationToken cancellationToken) => MarkAllDirtyAsync(corrected.TradingDate, cancellationToken);

    public async Task Handle(ReportGeneratedV1 generated, CancellationToken cancellationToken)
    {
        if (await store.FindPlannedReportAsync(generated.ReportId, cancellationToken) is null)
            store.AddPlannedReport(PlannedReport.Of(generated.ReportId, generated.AccountId, UserId.Of(generated.OwnerId), generated.Kind, generated.Period,
                generated.MarketAsOf, generated.GeneratedAt,
                [.. generated.Actions.Select(action => new PlannedAction(action.Kind, action.InstrumentId, action.Quantity, action.Price,
                    [.. action.Allocations.Select(part => new PlannedPart(part.SubPortfolioId, part.Quantity))], action.FromSubPortfolioId))]));
        await MarkDirtyAsync(generated.AccountId, generated.OwnerId, generated.MarketAsOf, cancellationToken);
    }

    public async Task Handle(ReportSupersededV1 superseded, CancellationToken cancellationToken)
    {
        if (await store.FindPlannedReportAsync(superseded.ReportId, cancellationToken) is not { } report)
            return;
        report.Supersede();
        await MarkDirtyAsync(report.AccountId, report.OwnerId.Value, report.MarketAsOf, cancellationToken);
    }

    private async Task MarkDirtyAsync(Guid accountId, Guid ownerId, DateOnly from, CancellationToken cancellationToken)
    {
        var marker = await store.MarkerAsync(accountId, UserId.Of(ownerId), cancellationToken);
        marker.MarkDirty(from);
        await store.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkAllDirtyAsync(DateOnly from, CancellationToken cancellationToken)
    {
        foreach (var marker in await store.MarkersAsync(cancellationToken))
            marker.MarkDirty(from);
        await store.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>After the nightly quotes (22:30), rebuild what got outdated so the dashboard opens instantly.</summary>
public sealed class RebuildSnapshotsJob(IPerformanceRepository store, SnapshotBuilder builder) : IScheduledJob
{
    public static readonly DailySchedule Schedule = DailySchedule.Weekdays(new TimeOnly(23, 15), "Europe/Warsaw");

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var marker in (await store.MarkersAsync(cancellationToken)).Where(marker => marker.IsDirty).ToList())
            await builder.EnsureFreshAsync(marker.AccountId, cancellationToken);
    }
}
