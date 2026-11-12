using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record PlanReviewDto(
    string Reviewer,
    string Verdict,
    string Summary,
    IReadOnlyList<ReviewFindingDto> Findings)
{
    public static PlanReviewDto From(PlanReview planReview) => new(
        Reviewer: planReview.Reviewer,
        Verdict: planReview.Verdict.ToString(),
        Summary: planReview.Summary,
        Findings: [.. planReview.Findings.Select(ReviewFindingDto.From)]);
}
