namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class GuardrailEnforcer
{
    public static (StrategyTargets Targets, IReadOnlyList<string> Warnings) Clip(
        StrategyTargets strategy, GuardrailLimits limits, Func<Guid, string> label)
    {
        var warnings = new List<string>();
        var targets = strategy.Targets.Where(target => target.Weight > 0m).ToList();

        if (limits.MaxPositionWeight is { } cap)
        {
            targets = [.. targets.Select(target =>
            {
                if (target.Weight <= cap.Value)
                    return target;

                warnings.Add($"Ціль {label(target.InstrumentId)} {PlanText.Percent(target.Weight)} обрізано до максимуму позиції {PlanText.Percent(cap.Value)}.");

                return target with { Weight = cap.Value };
            })];
        }

        var total = targets.Sum(target => target.Weight);
        if (total > 1m)
        {
            warnings.Add($"Цілі стратегії разом {PlanText.Percent(total)}: без плеча, тому їх пропорційно зменшено до 100%.");
            targets = [.. targets.Select(target => target with { Weight = target.Weight / total })];
        }

        return (strategy.WithTargets(targets), warnings);
    }

    public static string? CheckTurnover(SubPortfolioPlan plan, GuardrailLimits limits)
    {
        if (limits.MaxMonthlyTurnover is not { } max || plan.TotalBefore <= 0m)
            return null;

        var traded = plan.Orders.Where(order => order.Kind is OrderKind.Exit or OrderKind.Trim or OrderKind.Buy).Sum(order => order.Amount);
        var newMoney = plan.Contribution;
        var turnover = Math.Max(0m, traded - newMoney) / plan.TotalBefore;

        return turnover > max.Value
            ? $"Оборот плану {PlanText.Percent(turnover)} перевищує ліміт {PlanText.Percent(max.Value)} на місяць: перегляньте продажі перед виконанням."
            : null;
    }
}
