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
            if (OperationKindOf(action.Kind) is not { } kind)
                continue;

            var remaining = action.Kind == ActionKind.Contribute
                ? action.PlannedAmount - action.Fills.Sum(fill => fill.Amount)
                : action.RemainingQuantity;
            var picked = new List<JournalOperation>();

            foreach (var operation in candidates.Where(operation => operation.Kind == kind && !claimed.Contains(operation.TransactionId)
                                                                     && (kind == JournalOperationKind.Contribution || operation.InstrumentId == action.InstrumentId)))
            {
                if (remaining <= Tolerance)
                    break;

                picked.Add(operation);
                claimed.Add(operation.TransactionId);
                remaining -= operation.Quantity;
            }

            if (picked.Count > 0)
                matches.Add(new ActionMatch(action.Id, picked, action.Kind == ActionKind.Contribute || remaining <= Tolerance));
        }

        return matches;
    }

    private static JournalOperationKind? OperationKindOf(ActionKind kind) => kind switch
    {
        ActionKind.Buy or ActionKind.Park => JournalOperationKind.Buy,
        ActionKind.Sell or ActionKind.Unpark => JournalOperationKind.Sell,
        ActionKind.Contribute => JournalOperationKind.Contribution,
        _ => null,
    };
}
