namespace InvestmentLab.Rebalancing.Reports.Domain;

public enum JournalOperationKind
{
    Buy,
    Sell,
    Contribution,
}

/// <summary>
/// An entry already in the Portfolio journal (imported from a statement or entered by hand) that a report action can
/// be marked with. For a contribution the quantity is the amount and the price is 1, as in <see cref="Fill"/>.
/// </summary>
public sealed record JournalOperation(Guid TransactionId, DateOnly OccurredOn, JournalOperationKind Kind, Guid? InstrumentId, decimal Quantity, decimal Price, decimal Fee);

/// <summary>Operations that carry out (part of) one action; <see cref="Completes"/> when they cover what is left of the plan.</summary>
public sealed record ActionMatch(ReportActionId ActionId, IReadOnlyList<JournalOperation> Operations, bool Completes);

/// <summary>
/// "Load operations": finds which journal entries made after the report was generated are the plan being carried out.
/// A trade matches an action of the same instrument and direction, a payment into the account matches the
/// contribution; operations are taken oldest first until the rest of the plan is covered, and one operation never
/// serves two actions. Transfers between sub-portfolios have no broker side and are left to the user.
/// </summary>
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
