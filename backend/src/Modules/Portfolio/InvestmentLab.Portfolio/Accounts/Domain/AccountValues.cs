namespace InvestmentLab.Portfolio.Accounts.Domain;

/// <summary>Planned part of the account a sub-portfolio should hold; above zero, at most 100%.</summary>
public sealed record TargetShare
{
    public Percentage Value { get; }

    private TargetShare(Percentage value) => Value = value;

    public static TargetShare Of(Percentage value) =>
        value.Value > 0m ? new(value) : throw new ArgumentOutOfRangeException(nameof(value), value, "Target share must be above zero.");

    public static Result<TargetShare> TryFromPercent(decimal percent) =>
        percent is > 0m and <= 100m
            ? new TargetShare(Percentage.FromPercent(percent))
            : AccountErrors.TargetShareOutOfRange(percent);

    public override string ToString() => Value.ToString();
}

public enum ContributionPolicyKind
{
    /// <summary>New money goes to the sub-portfolios furthest below their target share.</summary>
    FillUnderweight,

    /// <summary>New money is split by target shares, whatever the drift.</summary>
    FixedSplit,
}

/// <param name="DriftBand">Drift beyond which an internal transfer between sub-portfolios is suggested.</param>
public sealed record ContributionPolicy(ContributionPolicyKind Kind, Percentage DriftBand)
{
    public static readonly Percentage DefaultDriftBand = Percentage.FromPercent(5m);

    public static ContributionPolicy Default => new(ContributionPolicyKind.FillUnderweight, DefaultDriftBand);
}

/// <summary>Hard limits the rebalancer must respect for a sub-portfolio. Leverage is never allowed.</summary>
public sealed record Guardrails(Percentage? MaxPositionWeight, Percentage? MaxMonthlyTurnover)
{
    public static Guardrails None => new(null, null);

    public static bool AllowLeverage => false;
}

/// <summary>The strategy a sub-portfolio follows: the latest version, or one pinned version.</summary>
public sealed record StrategyRef(Guid StrategyId, int? PinnedVersion)
{
    public static Result<StrategyRef> TryOf(Guid strategyId, int? pinnedVersion) =>
        strategyId == Guid.Empty ? AccountErrors.StrategyRequired()
        : pinnedVersion is <= 0 ? AccountErrors.StrategyVersionInvalid(pinnedVersion.Value)
        : new StrategyRef(strategyId, pinnedVersion);
}

public enum SubPortfolioStatus
{
    Active,
    Retired,
}

/// <summary>What is needed to create a sub-portfolio.</summary>
public sealed record SubPortfolioDraft(string Name, TargetShare TargetShare, StrategyRef? Strategy, Guid? CashInstrumentId, Guardrails Guardrails);
