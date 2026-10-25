namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>Hard limits of a sub-portfolio (from Portfolio). Leverage is never allowed: targets above 100% are scaled down.</summary>
public sealed record GuardrailLimits(Percentage? MaxPositionWeight, Percentage? MaxMonthlyTurnover)
{
    public static GuardrailLimits None => new(null, null);
}

/// <summary>Applies guardrails after any strategy (D5): clips targets before planning, flags turnover after it.</summary>
public static class GuardrailEnforcer
{
    /// <summary>Targets within the limits; what is cut stays unallocated (parked). Warnings say what was cut.</summary>
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

    /// <summary>Buys and sells of picks (parking excluded) against the sub-portfolio value; a warning when above the limit.</summary>
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
