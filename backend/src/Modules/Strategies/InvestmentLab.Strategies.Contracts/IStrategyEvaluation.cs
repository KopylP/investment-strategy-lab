namespace InvestmentLab.Strategies.Contracts;

/// <summary>Runs a configured strategy for one sub-portfolio. Results are targets and signals, never orders.</summary>
public interface IStrategyEvaluation
{
    Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken);

    Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken);
}

/// <param name="Version">Null: the latest version of the strategy.</param>
/// <param name="CashInstrumentId">Overrides the strategy's parking ETF (the sub-portfolio's own setting).</param>
public sealed record EvaluateStrategyRequest(
    Guid StrategyId,
    int? Version,
    SubPortfolioStateDto SubPortfolio,
    decimal Contribution,
    DateOnly AsOf,
    Guid? CashInstrumentId = null);

/// <param name="Cash">Negative cash is passed as zero.</param>
public sealed record SubPortfolioStateDto(string Currency, decimal Cash, IReadOnlyList<HoldingStateDto> Holdings);

public sealed record HoldingStateDto(Guid InstrumentId, IReadOnlyList<LotStateDto> Lots);

public sealed record LotStateDto(decimal Quantity, decimal UnitCost, DateOnly? AcquiredOn);

/// <summary>Either a value or an error; contracts carry no domain result types.</summary>
public sealed record StrategyEvaluationResult<T>(T? Value, string? ErrorCode = null, string? ErrorMessage = null)
    where T : class
{
    public bool IsSuccess => ErrorCode is null;
}

/// <param name="CashInstrumentId">Where money that is not invested is parked (the sub-portfolio's ETF or the strategy's).</param>
public sealed record StrategyDecisionDto(
    Guid StrategyId,
    int StrategyVersion,
    string StrategyCode,
    string EngineVersion,
    DateOnly AsOf,
    decimal InvestFraction,
    IReadOnlyList<TargetWeightDto> Targets,
    MarketRegimeDto Market,
    RationaleDto Rationale,
    IReadOnlyList<string> Warnings,
    Guid? CashInstrumentId = null);

/// <param name="Weight">Share of the sub-portfolio, 0..1.</param>
public sealed record TargetWeightDto(Guid InstrumentId, decimal Weight);

public sealed record MarketRegimeDto(
    bool IsFavorable,
    decimal? BenchmarkPrice,
    decimal? BenchmarkMovingAverage,
    decimal? VolatilityIndex,
    IReadOnlyList<string> Reasons);

public sealed record RationaleDto(string Summary, decimal RiskFreeRate, IReadOnlyList<InstrumentScoreDto> Scores);

/// <summary>One row of the "why" table.</summary>
public sealed record InstrumentScoreDto(
    Guid InstrumentId,
    string Name,
    string Group,
    decimal? Price,
    decimal? Momentum,
    decimal? Return1m,
    decimal? Return3m,
    decimal? Return12m,
    decimal? Volatility,
    decimal? Sharpe,
    decimal? Drawdown,
    bool AboveMovingAverage,
    DateOnly? LastDate,
    int? Rank,
    string Status,
    string Reason,
    decimal? Weight);

/// <param name="Status">ok, watch or act.</param>
public sealed record RiskAssessmentDto(
    Guid StrategyId,
    int StrategyVersion,
    string StrategyCode,
    string EngineVersion,
    DateOnly AsOf,
    string Status,
    IReadOnlyList<string> MarketAlerts,
    IReadOnlyList<PositionRiskDto> Positions,
    PortfolioRiskDto Portfolio,
    IReadOnlyList<RiskSignalDto> Signals,
    IReadOnlyList<string> Actions);

public sealed record PositionRiskDto(
    Guid InstrumentId,
    string Status,
    decimal Quantity,
    decimal AverageCost,
    decimal CurrentPrice,
    decimal Value,
    decimal PnlPct,
    decimal DrawdownFromHigh,
    bool AboveMovingAverage,
    decimal? MovingAverage,
    decimal? ExitPrice,
    string ExitRule,
    decimal? ExitDistance,
    decimal? StopPrice,
    decimal? MaExitPrice,
    decimal? TrailExitPrice,
    IReadOnlyList<string> Alerts,
    bool Sell);

public sealed record PortfolioRiskDto(decimal Value, decimal Cost, decimal? PnlPct, decimal? Drawdown, DateOnly? PeakDate, bool ExitLimitHit, IReadOnlyList<string> Alerts);

/// <param name="Kind">warn or exit.</param>
public sealed record RiskSignalDto(Guid InstrumentId, string Kind, string Reason, decimal? ExitPrice);

/// <summary>A new version of the strategy exists; sub-portfolios following the latest version use it from now on.</summary>
public sealed record StrategyReconfiguredV1(Guid StrategyId, Guid OwnerId, int Version, DateTimeOffset ReconfiguredAt);

/// <summary>The user's strategies and what they may hold (e.g. to place an imported instrument in the right sub-portfolio).</summary>
public interface IStrategyDirectory
{
    Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken);
}

public sealed record StrategySummaryDto(Guid Id, string Name, string TypeCode, int LatestVersion, IReadOnlyList<Guid> Universe, Guid? CashInstrumentId, StrategyReportingDto Reporting);

/// <summary>Which reports the strategy needs: rebalance Weekly/Monthly/Quarterly (its frequency), risk review Off/Weekly (its "reporting" parameters).</summary>
public sealed record StrategyReportingDto(string Rebalance, string RiskReview)
{
    public static StrategyReportingDto Default => new("Monthly", "Weekly");
}
