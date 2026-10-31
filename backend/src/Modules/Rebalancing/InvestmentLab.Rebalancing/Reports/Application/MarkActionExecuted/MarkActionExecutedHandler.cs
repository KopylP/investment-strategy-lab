using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.MarkActionExecuted;

public sealed record MarkActionExecutedCommand(
    decimal? Quantity = null,
    decimal? Price = null,
    decimal Fee = 0m,
    DateOnly? ExecutedOn = null,
    string? Note = null);

public sealed class MarkActionExecutedHandler(FillMarker marker)
{
    public Task<Result> Handle(Guid reportId, Guid actionId, MarkActionExecutedCommand command, CancellationToken cancellationToken) =>
        marker.MarkAsync(reportId, actionId, (report, action, now) => MarkExecuted(report, action, command, now), cancellationToken);

    private static Result<MarkedFill> MarkExecuted(
        RebalancingReport report,
        ReportAction action,
        MarkActionExecutedCommand command,
        DateTimeOffset now) =>
        report.MarkExecuted(
            action.Id,
            command.Quantity ?? RemainingOrPlanned(action),
            command.Price ?? action.PlannedPrice,
            command.Fee,
            command.ExecutedOn ?? FillMarker.DayOf(now),
            command.Note,
            now);

    private static decimal RemainingOrPlanned(ReportAction action) =>
        action.RemainingQuantity > 0m ? action.RemainingQuantity : action.PlannedQuantity;
}
