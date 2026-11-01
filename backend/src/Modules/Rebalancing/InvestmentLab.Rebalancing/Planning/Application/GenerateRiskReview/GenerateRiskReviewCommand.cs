using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;

public sealed record GenerateRiskReviewCommand(Guid AccountId, ReportScope Scope, DateOnly? AsOf = null);
