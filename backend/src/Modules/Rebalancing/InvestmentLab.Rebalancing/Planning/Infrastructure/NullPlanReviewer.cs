using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class NullPlanReviewer : IPlanReviewer
{
    public Task<PlanReview?> ReviewAsync(ReportDraft plan, CancellationToken cancellationToken) => Task.FromResult<PlanReview?>(null);
}
