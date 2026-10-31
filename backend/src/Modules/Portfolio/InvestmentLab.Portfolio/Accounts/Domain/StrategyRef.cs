namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record StrategyRef(Guid StrategyId, int? PinnedVersion)
{
    public static Result<StrategyRef> TryOf(Guid strategyId, int? pinnedVersion) =>
        strategyId == Guid.Empty ? AccountErrors.StrategyRequired()
        : pinnedVersion is <= 0 ? AccountErrors.StrategyVersionInvalid(pinnedVersion.Value)
        : new StrategyRef(strategyId, pinnedVersion);
}
