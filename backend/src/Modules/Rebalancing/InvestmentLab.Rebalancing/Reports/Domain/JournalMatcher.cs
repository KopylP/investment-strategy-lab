namespace InvestmentLab.Rebalancing.Reports.Domain;

public static class JournalMatcher
{
    private const decimal Tolerance = 0.0001m;

    public static IReadOnlyList<ActionMatch> Match(RebalancingReport report, IReadOnlyList<JournalOperation> operations)
    {
        if (!report.IsOpen)
            return [];

        var generatedOn = DateOnly.FromDateTime(report.GeneratedAt.UtcDateTime);
        var candidates = operations.Where(operation => operation.OccurredOn >= generatedOn).OrderBy(operation => operation.OccurredOn).ToList();
        var claimed = new HashSet<Guid>();
        var matches = new List<ActionMatch>();

        foreach (var action in report.Actions.Where(action => action.Status is ActionStatus.Pending or ActionStatus.PartiallyExecuted))
        {
            if (MatchOf(action, candidates, claimed) is { } match)
                matches.Add(match);
        }

        return matches;
    }

    private static ActionMatch? MatchOf(ReportAction action, List<JournalOperation> candidates, HashSet<Guid> claimed)
    {
        if (OperationKindOf(action.Kind) is not { } kind)
            return null;

        var remaining = RemainingOf(action);
        var picked = new List<JournalOperation>();

        foreach (var operation in candidates.Where(operation => Fits(operation, action, kind, claimed)))
        {
            if (remaining <= Tolerance)
                break;

            picked.Add(operation);
            claimed.Add(operation.TransactionId);
            remaining -= operation.Quantity;
        }

        if (picked.Count == 0)
            return null;

        return new ActionMatch(action.Id, picked, action.Kind == ActionKind.Contribute || remaining <= Tolerance);
    }

    private static decimal RemainingOf(ReportAction action) =>
        action.Kind == ActionKind.Contribute
            ? action.PlannedAmount - action.Fills.Sum(fill => fill.Amount)
            : action.RemainingQuantity;

    private static bool Fits(JournalOperation operation, ReportAction action, JournalOperationKind kind, HashSet<Guid> claimed) =>
        operation.Kind == kind
        && !claimed.Contains(operation.TransactionId)
        && (kind == JournalOperationKind.Contribution || operation.InstrumentId == action.InstrumentId);

    private static JournalOperationKind? OperationKindOf(ActionKind kind) => kind switch
    {
        ActionKind.Buy or ActionKind.Park => JournalOperationKind.Buy,
        ActionKind.Sell or ActionKind.Unpark => JournalOperationKind.Sell,
        ActionKind.Contribute => JournalOperationKind.Contribution,
        _ => null,
    };
}
