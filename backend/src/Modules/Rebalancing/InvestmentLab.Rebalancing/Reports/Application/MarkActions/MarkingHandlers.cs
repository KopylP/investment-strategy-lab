using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.MarkActions;

/// <summary>Everything optional defaults to the plan: the rest of the planned quantity, the planned price, today.</summary>
public sealed record MarkActionExecutedCommand(decimal? Quantity = null, decimal? Price = null, decimal Fee = 0m, DateOnly? ExecutedOn = null, string? Note = null);

public sealed record RecordPartialFillCommand(decimal Quantity, decimal? Price = null, decimal Fee = 0m, DateOnly? ExecutedOn = null, string? Note = null);

public sealed record SkipActionCommand(string Reason);

/// <summary>
/// Marks made by the user in a report (§7.2). The report changes and the command for Portfolio (record or reverse the
/// journal entry) is stored in the outbox in the same transaction; Portfolio answers with recorded/rejected events.
/// </summary>
public sealed class MarkingHandler(IReportRepository reports, TimeProvider clock)
{
    public Task<Result> MarkExecuted(Guid reportId, Guid actionId, MarkActionExecutedCommand command, CancellationToken cancellationToken) =>
        MarkAsync(reportId, actionId, (report, action, now) => report.MarkExecuted(
            action.Id,
            command.Quantity ?? DefaultQuantity(action),
            command.Price ?? action.PlannedPrice,
            command.Fee,
            command.ExecutedOn ?? Today(now),
            command.Note,
            now), cancellationToken);

    public Task<Result> RecordPartialFill(Guid reportId, Guid actionId, RecordPartialFillCommand command, CancellationToken cancellationToken) =>
        MarkAsync(reportId, actionId, (report, action, now) => report.RecordPartialFill(
            action.Id,
            command.Quantity,
            command.Price ?? action.PlannedPrice,
            command.Fee,
            command.ExecutedOn ?? Today(now),
            command.Note,
            now), cancellationToken);

    public async Task<Result> Skip(Guid reportId, Guid actionId, SkipActionCommand command, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var now = clock.GetUtcNow();
        var wasOpen = report.IsOpen;
        var result = report.Skip(ReportActionId.Of(actionId), command.Reason, now);
        if (result.IsFailure)
            return result;

        PublishClosedIfJustClosed(report, wasOpen, now);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Undo(Guid reportId, Guid actionId, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var undone = report.UndoMark(ReportActionId.Of(actionId));
        if (undone.IsFailure)
            return undone.Error;

        var now = clock.GetUtcNow();
        // A fill linked to an entry that was already in the journal is only unlinked: the entry is the broker's truth.
        foreach (var fill in undone.Value.Where(marked => !marked.Fill.Linked))
            reports.Publish(new ReverseTransactionV1(report.AccountId, fill.Reference.ToString(), "відмітку в рапорті скасовано"), now);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> Close(Guid reportId, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var now = clock.GetUtcNow();
        var closed = report.Close(now);
        if (closed.IsFailure)
            return closed;

        reports.Publish(new ReportClosedV1(report.Id.Value, report.AccountId, report.OwnerId.Value, now), now);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> MarkAsync(
        Guid reportId, Guid actionId, Func<RebalancingReport, ReportAction, DateTimeOffset, Result<MarkedFill>> mark, CancellationToken cancellationToken)
    {
        if (await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);
        if (report.FindAction(ReportActionId.Of(actionId)) is not { } action)
            return ReportErrors.ActionNotFound(actionId);

        var now = clock.GetUtcNow();
        var wasOpen = report.IsOpen;
        var marked = mark(report, action, now);
        if (marked.IsFailure)
            return marked.Error;

        FillCommands.Publish(reports, report, marked.Value, now);
        PublishClosedIfJustClosed(report, wasOpen, now);
        await reports.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private void PublishClosedIfJustClosed(RebalancingReport report, bool wasOpen, DateTimeOffset now)
    {
        if (wasOpen && report.Status == ReportStatus.Closed)
            reports.Publish(new ReportClosedV1(report.Id.Value, report.AccountId, report.OwnerId.Value, now), now);
    }

    private static decimal DefaultQuantity(ReportAction action) => action.RemainingQuantity > 0m ? action.RemainingQuantity : action.PlannedQuantity;

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}

/// <summary>The Portfolio command that records one fill in the journal.</summary>
internal static class FillCommands
{
    public static void Publish(IReportRepository reports, RebalancingReport report, MarkedFill marked, DateTimeOffset now)
    {
        var (action, fill, reference) = (marked.Action, marked.Fill, marked.Reference.ToString());
        switch (action.Kind)
        {
            case ActionKind.Contribute:
                reports.Publish(new RecordContributionV1(
                    report.AccountId, reference, fill.Amount, fill.ExecutedOn,
                    [.. action.AllocateFill(fill.Amount).Select(part => new AmountAllocationDto(part.SubPortfolioId, decimal.Round(part.Quantity, 2)))]), now);
                break;
            case ActionKind.Transfer:
                reports.Publish(new RecordInternalTransferV1(
                    report.AccountId, reference, action.FromSubPortfolioId!.Value, action.ToSubPortfolioId!.Value, fill.ExecutedOn,
                    action.InstrumentId, fill.Quantity, null), now);
                break;
            default:
                reports.Publish(new RecordTradeV1(
                    report.AccountId, reference, action.InstrumentId!.Value,
                    action.Kind is ActionKind.Sell or ActionKind.Unpark ? "Sell" : "Buy",
                    fill.Quantity, fill.Price, fill.Fee, fill.ExecutedOn,
                    [.. action.AllocateFill(fill.Quantity).Select(part => new QuantityAllocationDto(part.SubPortfolioId, part.Quantity))]), now);
                break;
        }
    }
}
