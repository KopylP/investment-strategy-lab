namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record SubPortfolioDraft(string Name, TargetShare TargetShare, StrategyRef? Strategy, Guid? CashInstrumentId, Guardrails Guardrails)
{
    public static Result<SubPortfolioDraft> TryOf(NewSubPortfolio asked)
    {
        var share = TargetShare.TryFromPercent(asked.TargetSharePercent);
        if (share.IsFailure)
            return share.Error;

        var strategy = StrategyOf(asked);
        if (strategy.IsFailure)
            return strategy.Error;

        var maxWeight = Percentage.TryFromOptionalPercent(asked.MaxPositionWeightPercent);
        if (maxWeight.IsFailure)
            return maxWeight.Error;

        var maxTurnover = Percentage.TryFromOptionalPercent(asked.MaxMonthlyTurnoverPercent);
        if (maxTurnover.IsFailure)
            return maxTurnover.Error;

        return new SubPortfolioDraft(
            Name: asked.Name ?? "",
            TargetShare: share.Value,
            Strategy: strategy.Value,
            CashInstrumentId: asked.CashInstrumentId,
            Guardrails: new Guardrails(maxWeight.Value, maxTurnover.Value));
    }

    private static Result<StrategyRef?> StrategyOf(NewSubPortfolio asked)
    {
        if (asked.StrategyId is not { } strategyId)
            return Result<StrategyRef?>.Success(null);

        var reference = StrategyRef.TryOf(strategyId, asked.PinnedStrategyVersion);

        return reference.IsFailure ? reference.Error : reference.Value;
    }
}
