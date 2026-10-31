namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionRisk(
    string Status,
    IReadOnlyList<string> MarketAlerts,
    IReadOnlyList<SectionRiskPosition> Positions,
    IReadOnlyList<string> PortfolioAlerts,
    IReadOnlyList<string> Actions);
