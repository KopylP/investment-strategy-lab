using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReviewFindingDto(
    string Severity,
    string Message)
{
    public static ReviewFindingDto From(ReviewFinding reviewFinding) => new(
        Severity: reviewFinding.Severity.ToString(),
        Message: reviewFinding.Message);
}
