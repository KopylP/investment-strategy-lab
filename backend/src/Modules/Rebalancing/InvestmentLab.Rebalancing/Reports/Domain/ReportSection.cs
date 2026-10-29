using System.Text.Json;

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
/// <param name="ScoreTitle">Heading of the table; null in reports made before strategies named their columns.</param>
/// <param name="Columns">The strategy's own columns of the table, in order; null in those older reports.</param>
public sealed record SectionDecision(
    bool MarketIsFavorable,
    decimal InvestFraction,
    IReadOnlyList<string> MarketReasons,
    string Summary,
    IReadOnlyList<SectionTarget> Targets,
    IReadOnlyList<SectionScore> Scores,
    IReadOnlyList<string> Warnings,
    string? ScoreTitle = null,
    string? InstrumentLabel = null,
    IReadOnlyList<SectionColumn>? Columns = null);

/// <param name="Format">percent, signedPercent, number, money, trend or text.</param>
public sealed record SectionColumn(string Id, string Label, string Format, string? Hint, bool Compact, string? TrueHint, string? FalseHint);

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
    bool? AboveMovingAverage,
    int? Rank,
    string Status,
    string Reason,
    IReadOnlyDictionary<string, JsonElement>? Values = null);

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
