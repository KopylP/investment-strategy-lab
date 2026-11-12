using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Accounts.Application.Shared;

public sealed record NewSubPortfolioDto(
    string Name,
    decimal TargetSharePercent,
    Guid? StrategyId = null,
    int? PinnedStrategyVersion = null,
    Guid? CashInstrumentId = null,
    decimal? MaxPositionWeightPercent = null,
    decimal? MaxMonthlyTurnoverPercent = null)
{
    public NewSubPortfolio ToDomain() => new(
        Name: Name,
        TargetSharePercent: TargetSharePercent,
        StrategyId: StrategyId,
        PinnedStrategyVersion: PinnedStrategyVersion,
        CashInstrumentId: CashInstrumentId,
        MaxPositionWeightPercent: MaxPositionWeightPercent,
        MaxMonthlyTurnoverPercent: MaxMonthlyTurnoverPercent);
}
