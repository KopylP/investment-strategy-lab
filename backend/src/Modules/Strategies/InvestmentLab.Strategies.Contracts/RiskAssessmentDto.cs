namespace InvestmentLab.Strategies.Contracts;

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
