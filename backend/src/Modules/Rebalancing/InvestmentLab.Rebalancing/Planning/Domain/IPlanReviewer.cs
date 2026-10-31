using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IPlanReviewer
{
    Task<PlanReview?> ReviewAsync(ReportDraft plan, CancellationToken cancellationToken);
}
