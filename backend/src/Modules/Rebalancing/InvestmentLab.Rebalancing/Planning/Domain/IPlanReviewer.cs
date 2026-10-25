using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>Port for a plan reviewer. Null: no review (the default until the AI reviewer exists).</summary>
public interface IPlanReviewer
{
    Task<PlanReview?> ReviewAsync(ReportDraft plan, CancellationToken cancellationToken);
}
