using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Shadow.Domain;

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
        var parts = plans.SelectMany(plan => plan.Actions.SelectMany(action => action.Allocations.Select(part => (plan, action, part))));
        foreach (var (plan, action, part) in parts)
        {
            var amount = part.Quantity * action.Price;
            var movement = action.Kind switch
            {
                "Contribute" => new Movement(plan.MarketAsOf, sequence++, part.SubPortfolioId, null, 0m, part.Quantity, part.Quantity),
                "Buy" or "Park" => new Movement(
                    plan.MarketAsOf,
                    sequence++,
                    part.SubPortfolioId,
                    action.InstrumentId,
                    part.Quantity,
                    -amount,
                    0m,
                    TradePrice: action.Price),
                "Sell" or "Unpark" => new Movement(
                    plan.MarketAsOf,
                    sequence++,
                    part.SubPortfolioId,
                    action.InstrumentId,
                    -part.Quantity,
                    amount,
                    0m,
                    TradePrice: action.Price),
                _ => null,
            };

            if (movement is not null)
                movements.Add(movement);

            if (action is { Kind: "Transfer", FromSubPortfolioId: { } source })
            {
                movements.Add(new Movement(
                    plan.MarketAsOf,
                    sequence++,
                    source,
                    action.InstrumentId,
                    -part.Quantity,
                    0m,
                    0m,
                    TransferQuantity: -part.Quantity));
                movements.Add(new Movement(
                    plan.MarketAsOf,
                    sequence++,
                    part.SubPortfolioId,
                    action.InstrumentId,
                    part.Quantity,
                    0m,
                    0m,
                    TransferQuantity: part.Quantity));
            }
        }

        return movements;
    }
}
