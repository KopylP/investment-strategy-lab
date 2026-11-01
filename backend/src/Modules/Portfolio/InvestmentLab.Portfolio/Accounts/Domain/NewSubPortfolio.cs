namespace InvestmentLab.Portfolio.Accounts.Domain;

public sealed record NewSubPortfolio(
    string Name,
    decimal TargetSharePercent,
    Guid? StrategyId = null,
    int? PinnedStrategyVersion = null,
    Guid? CashInstrumentId = null,
    decimal? MaxPositionWeightPercent = null,
    decimal? MaxMonthlyTurnoverPercent = null);
