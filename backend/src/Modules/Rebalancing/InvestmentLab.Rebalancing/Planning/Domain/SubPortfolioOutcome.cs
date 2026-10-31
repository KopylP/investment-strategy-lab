using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record SubPortfolioOutcome(
    Guid SubPortfolioId,
    string Name,
    decimal TargetShare,
    decimal Contribution,
    StrategyInfo? Strategy,
    SectionDecision? Decision,
    SubPortfolioPlan? Plan,
    IReadOnlyList<PlannedOrder> Orders,
    IReadOnlyList<string> Notes,
    SectionRisk? Risk = null,
    string? Error = null);
