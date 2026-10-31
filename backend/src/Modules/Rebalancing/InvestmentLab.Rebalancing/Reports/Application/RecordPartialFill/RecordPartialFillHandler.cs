using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.RecordPartialFill;

public sealed record RecordPartialFillCommand(
    decimal Quantity,
    decimal? Price = null,
    decimal Fee = 0m,
    DateOnly? ExecutedOn = null,
    string? Note = null);

public sealed class RecordPartialFillHandler(FillMarker marker)
{
    public Task<Result> Handle(Guid reportId, Guid actionId, RecordPartialFillCommand command, CancellationToken cancellationToken) =>
        marker.MarkAsync(reportId, actionId, (report, action, now) => RecordFill(report, action, command, now), cancellationToken);

    private static Result<MarkedFill> RecordFill(
        RebalancingReport report,
        ReportAction action,
        RecordPartialFillCommand command,
        DateTimeOffset now) =>
        report.RecordPartialFill(
            action.Id,
            command.Quantity,
            command.Price ?? action.PlannedPrice,
            command.Fee,
            command.ExecutedOn ?? FillMarker.DayOf(now),
            command.Note,
            now);
}
