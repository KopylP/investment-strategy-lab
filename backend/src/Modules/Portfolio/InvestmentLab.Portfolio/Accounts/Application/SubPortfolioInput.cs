namespace InvestmentLab.Portfolio.Accounts.Application;

public sealed record SubPortfolioInput(
    string Name,
    decimal TargetSharePercent,
    Guid? StrategyId = null,
    int? PinnedStrategyVersion = null,
    Guid? CashInstrumentId = null,
    decimal? MaxPositionWeightPercent = null,
    decimal? MaxMonthlyTurnoverPercent = null);
