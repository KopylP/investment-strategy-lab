namespace InvestmentLab.Portfolio.Contracts.Accounts;

public sealed record SubPortfolioSetupDto(
    Guid Id,
    string Name,
    decimal TargetShare,
    string Status,
    Guid? StrategyId,
    int? PinnedStrategyVersion,
    Guid? CashInstrumentId,
    decimal? MaxPositionWeight,
    decimal? MaxMonthlyTurnover);
