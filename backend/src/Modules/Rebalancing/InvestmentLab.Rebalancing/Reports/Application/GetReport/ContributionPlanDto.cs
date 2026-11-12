using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ContributionPlanDto(
    decimal Total,
    string Split,
    IReadOnlyList<ContributionPartDto> Parts,
    TransferHintDto? Transfer)
{
    public static ContributionPlanDto From(ContributionPlan contributionPlan) => new(
        Total: contributionPlan.Total,
        Split: contributionPlan.Split,
        Parts: [.. contributionPlan.Parts.Select(ContributionPartDto.From)],
        Transfer: contributionPlan.Transfer is { } transfer ? TransferHintDto.From(transfer) : null);
}
