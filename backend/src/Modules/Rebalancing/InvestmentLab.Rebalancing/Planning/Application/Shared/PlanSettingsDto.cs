using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule, bool LevelByPurchasesOnly)
{
    public static PlanSettingsDto Of(PlanSettings settings) => new(
        settings.AccountId,
        settings.MonthlyContribution,
        settings.MinTrade,
        settings.TrimBand.Value,
        settings.AllowFractionalShares,
        settings.GenerateOnSchedule,
        settings.LevelByPurchasesOnly);
}
