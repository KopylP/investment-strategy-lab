using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Shadow.Domain;

/// <param name="Kind">Contribute, Sell, Unpark, Transfer, Buy or Park; for Contribute the quantity is the amount.</param>
public sealed record PlannedAction(string Kind, Guid? InstrumentId, decimal Quantity, decimal Price, IReadOnlyList<PlannedPart> Allocations, Guid? FromSubPortfolioId);

public sealed record PlannedPart(Guid SubPortfolioId, decimal Quantity);

/// <summary>Performance's copy of a generated report: what the plan said to do, for the "everything as planned" portfolio.</summary>
public sealed class PlannedReport
{
    public Guid ReportId { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Kind { get; private set; }
    public string Period { get; private set; }
    public DateOnly MarketAsOf { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public List<PlannedAction> Actions { get; private set; }
    public bool IsSuperseded { get; private set; }

    private PlannedReport(Guid reportId, Guid accountId, UserId ownerId, string kind, string period, DateOnly marketAsOf, DateTimeOffset generatedAt, IReadOnlyList<PlannedAction> actions) =>
        (ReportId, AccountId, OwnerId, Kind, Period, MarketAsOf, GeneratedAt, Actions) = (reportId, accountId, ownerId, kind, period, marketAsOf, generatedAt, [.. actions]);

    // EF Core.
    private PlannedReport() => (Kind, Period, Actions) = (null!, null!, null!);

    public static PlannedReport Of(Guid reportId, Guid accountId, UserId ownerId, string kind, string period, DateOnly marketAsOf, DateTimeOffset generatedAt, IReadOnlyList<PlannedAction> actions) =>
        new(reportId, accountId, ownerId, kind, period, marketAsOf, generatedAt, actions);

    public void Supersede() => IsSuperseded = true;

    public bool CountsForPlan => !IsSuperseded && Kind is "Monthly" or "Recalculation";
}

/// <summary>
/// "Everything as planned" (§6.8 ShadowPortfolioSimulator): the real journal until the first plan, then every
/// non-superseded plan executed in full at its planned prices on its market date, and nothing else.
/// </summary>
public static class ShadowPortfolioSimulator
{
    public static IReadOnlyList<Movement> Movements(IReadOnlyList<Movement> actual, IReadOnlyList<PlannedReport> reports)
    {
        var plans = reports.Where(report => report.CountsForPlan).OrderBy(report => report.MarketAsOf).ThenBy(report => report.GeneratedAt).ToList();
        if (plans.Count == 0)
            return actual;

        var start = plans[0].MarketAsOf;
        var movements = actual.Where(movement => movement.Date < start).ToList();
        var sequence = movements.Count == 0 ? 0 : movements.Max(movement => movement.Sequence) + 1;
        foreach (var plan in plans)
        foreach (var action in plan.Actions)
        foreach (var part in action.Allocations)
        {
            var amount = part.Quantity * action.Price;
            Movement? movement = action.Kind switch
            {
                "Contribute" => new Movement(plan.MarketAsOf, sequence++, part.SubPortfolioId, null, 0m, part.Quantity, part.Quantity),
                "Buy" or "Park" => new Movement(plan.MarketAsOf, sequence++, part.SubPortfolioId, action.InstrumentId, part.Quantity, -amount, 0m, TradePrice: action.Price),
                "Sell" or "Unpark" => new Movement(plan.MarketAsOf, sequence++, part.SubPortfolioId, action.InstrumentId, -part.Quantity, amount, 0m, TradePrice: action.Price),
                _ => null,
            };
            if (movement is not null)
                movements.Add(movement);
            if (action is { Kind: "Transfer", FromSubPortfolioId: { } source })
            {
                movements.Add(new Movement(plan.MarketAsOf, sequence++, source, action.InstrumentId, -part.Quantity, 0m, 0m, TransferQuantity: -part.Quantity));
                movements.Add(new Movement(plan.MarketAsOf, sequence++, part.SubPortfolioId, action.InstrumentId, part.Quantity, 0m, 0m, TransferQuantity: part.Quantity));
            }
        }
        return movements;
    }
}
