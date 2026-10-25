namespace InvestmentLab.Rebalancing.Reports.Domain;

/// <summary>
/// What a report says about one sub-portfolio: which strategy (version) decided, why, and the orders and allocation
/// before netting. A snapshot: it never changes after the report is generated.
/// </summary>
public sealed record ReportSection(
    Guid SubPortfolioId,
    string Name,
    decimal TargetShare,
    Guid? StrategyId,
    int? StrategyVersion,
    string? StrategyCode,
    string? EngineVersion,
    decimal Contribution,
    SectionDecision? Decision,
    IReadOnlyList<SectionOrder> Orders,
    IReadOnlyList<SectionAllocation> Allocation,
    decimal TotalBefore,
    decimal TotalAfter,
    decimal ParkedBefore,
    decimal ParkedAfter,
    IReadOnlyList<string> Notes,
    SectionRisk? Risk,
    string? Error);

/// <param name="Scores">The "why" table: one row per scored instrument.</param>
public sealed record SectionDecision(
    bool MarketIsFavorable,
    decimal InvestFraction,
    IReadOnlyList<string> MarketReasons,
    string Summary,
    IReadOnlyList<SectionTarget> Targets,
    IReadOnlyList<SectionScore> Scores,
    IReadOnlyList<string> Warnings);

public sealed record SectionTarget(Guid InstrumentId, decimal Weight);

public sealed record SectionScore(
    Guid InstrumentId,
    string Name,
    string Group,
    decimal? Price,
    decimal? Momentum,
    decimal? Return1m,
    decimal? Return3m,
    decimal? Return12m,
    decimal? Volatility,
    bool AboveMovingAverage,
    int? Rank,
    string Status,
    string Reason);

/// <param name="Kind">Exit, Trim, Unpark, Buy or Park (before netting).</param>
public sealed record SectionOrder(string Kind, Guid InstrumentId, decimal Quantity, decimal Price, decimal Amount, string Reason);

/// <param name="Role">Pick, Exit or Cash.</param>
public sealed record SectionAllocation(
    Guid? InstrumentId, string Role, decimal ValueBefore, decimal ValueAfter, decimal WeightBefore, decimal WeightAfter, decimal? TargetWeight);

/// <summary>Weekly risk check of a sub-portfolio.</summary>
/// <param name="Status">ok, watch or act.</param>
public sealed record SectionRisk(
    string Status,
    IReadOnlyList<string> MarketAlerts,
    IReadOnlyList<SectionRiskPosition> Positions,
    IReadOnlyList<string> PortfolioAlerts,
    IReadOnlyList<string> Actions);

public sealed record SectionRiskPosition(
    Guid InstrumentId,
    string Status,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal Value,
    decimal PnlPct,
    decimal DrawdownFromHigh,
    decimal? ExitPrice,
    string ExitRule,
    decimal? ExitDistance,
    IReadOnlyList<string> Alerts,
    bool Sell);
