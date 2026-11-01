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

                warnings.Add(Message.Of("report.warning.target_capped", new { label = label(target.InstrumentId), target = target.Weight, cap = cap.Value }));

                return target with { Weight = cap.Value };
            })];
        }

        var total = targets.Sum(target => target.Weight);
        if (total > 1m)
        {
            warnings.Add(Message.Of("report.warning.targets_scaled", new { total }));
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
            ? Message.Of("report.warning.turnover", new { turnover, limit = max.Value })
            : null;
    }
}
