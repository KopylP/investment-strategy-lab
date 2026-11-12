using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionRiskDto(
    string Status,
    IReadOnlyList<string> MarketAlerts,
    IReadOnlyList<SectionRiskPositionDto> Positions,
    IReadOnlyList<string> PortfolioAlerts,
    IReadOnlyList<string> Actions)
{
    public static SectionRiskDto From(SectionRisk sectionRisk) => new(
        Status: sectionRisk.Status,
        MarketAlerts: sectionRisk.MarketAlerts,
        Positions: [.. sectionRisk.Positions.Select(SectionRiskPositionDto.From)],
        PortfolioAlerts: sectionRisk.PortfolioAlerts,
        Actions: sectionRisk.Actions);
}
