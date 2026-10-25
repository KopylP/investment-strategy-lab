using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.LoadOperations;

public sealed record MatchedOperationDto(Guid TransactionId, DateOnly OccurredOn, string Kind, Guid? InstrumentId, decimal Quantity, decimal Price, decimal Fee, string Source);

public sealed record ActionMatchDto(Guid ActionId, int Sequence, string ActionKind, Guid? InstrumentId, decimal PlannedQuantity, decimal RemainingQuantity, bool Completes, IReadOnlyList<MatchedOperationDto> Operations);

/// <param name="Unmatched">Operations after the report that no action wanted (off-plan trades), for the user to see.</param>
public sealed record LoadOperationsPreviewDto(IReadOnlyList<ActionMatchDto> Matches, IReadOnlyList<MatchedOperationDto> Unmatched);

/// <summary>
/// "Load operations" in a report: trades and payments already in the journal (a Revolut statement import, manual
/// entries) that carry out the plan mark its actions, without a second journal entry.
/// </summary>
public sealed class LoadOperationsHandler(IReportRepository reports, IJournalQueries journal, TimeProvider clock)
{
    public async Task<Result<LoadOperationsPreviewDto>> Preview(Guid reportId, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(reportId, cancellationToken);
        if (context.IsFailure)
            return context.Error;

        var (report, operations, sources) = context.Value;
        var matches = JournalMatcher.Match(report, operations);
        var used = matches.SelectMany(match => match.Operations).Select(operation => operation.TransactionId).ToHashSet();
        var generatedOn = DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime);

        return new LoadOperationsPreviewDto(
            [.. matches.Select(match =>
            {
                var action = report.FindAction(match.ActionId)!;
                return new ActionMatchDto(action.Id.Value, action.Sequence, action.Kind.ToString(), action.InstrumentId, action.PlannedQuantity,
                    action.RemainingQuantity, match.Completes, [.. match.Operations.Select(operation => Dto(operation, sources))]);
            })],
            [.. operations.Where(operation => operation.OccurredOn >= generatedOn && !used.Contains(operation.TransactionId)).Select(operation => Dto(operation, sources))]);
    }

    public async Task<Result<int>> Apply(Guid reportId, CancellationToken cancellationToken)
    {
        var context = await LoadAsync(reportId, cancellationToken);
        if (context.IsFailure)
            return context.Error;

        var report = context.Value.Report;
        var matches = JournalMatcher.Match(report, context.Value.Operations);
        var now = clock.GetUtcNow();
        var wasOpen = report.IsOpen;
        var linked = report.LinkJournalOperations(matches, now);
        if (linked.IsFailure)
            return linked.Error;

        if (wasOpen && report.Status == ReportStatus.Closed)
            reports.Publish(new ReportClosedV1(report.Id.Value, report.AccountId, report.OwnerId.Value, now), now);
        await reports.SaveChangesAsync(cancellationToken);
        return matches.Count;
    }

    private async Task<Result<(RebalancingReport Report, IReadOnlyList<JournalOperation> Operations, IReadOnlyDictionary<Guid, string> Sources)>> LoadAsync(
        Guid reportId, CancellationToken cancellationToken)
    {
        if (reportId == Guid.Empty || await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);
        if (!report.IsOpen)
            return ReportErrors.ReportIsNotOpen(report.Id.Value, report.Status);

        var snapshot = await journal.GetJournal(report.AccountId, cancellationToken);
        if (snapshot is null)
            return ReportErrors.NotFound(reportId);

        // Entries any report already points at (its own fills or earlier links) are not offered again.
        var taken = (await reports.ListAsync(report.AccountId, cancellationToken))
            .SelectMany(each => each.Actions).SelectMany(action => action.Fills)
            .Select(fill => fill.TransactionId).OfType<Guid>().ToHashSet();
        var reversed = snapshot.ReversedIds.ToHashSet();

        var live = snapshot.Entries
            .Where(entry => entry.Source is "Import" or "Manual" && !reversed.Contains(entry.TransactionId) && !taken.Contains(entry.TransactionId))
            .ToList();
        var operations = live.Select(ToOperation).OfType<JournalOperation>().ToList();
        return (report, operations, live.ToDictionary(entry => entry.TransactionId, entry => entry.Source));
    }

    private static JournalOperation? ToOperation(TransactionRecordedV1 entry) => entry switch
    {
        { Trade: { } trade } => new JournalOperation(entry.TransactionId, entry.OccurredOn,
            trade.Side == "Sell" ? JournalOperationKind.Sell : JournalOperationKind.Buy, trade.InstrumentId, trade.Quantity, trade.Price, trade.Fee),
        { Cash: { Kind: "Contribution" } cash } => new JournalOperation(entry.TransactionId, entry.OccurredOn, JournalOperationKind.Contribution, null, cash.Amount, 1m, 0m),
        _ => null,
    };

    private static MatchedOperationDto Dto(JournalOperation operation, IReadOnlyDictionary<Guid, string> sources) => new(
        operation.TransactionId, operation.OccurredOn, operation.Kind.ToString(), operation.InstrumentId, operation.Quantity, operation.Price, operation.Fee,
        sources.GetValueOrDefault(operation.TransactionId, ""));
}
