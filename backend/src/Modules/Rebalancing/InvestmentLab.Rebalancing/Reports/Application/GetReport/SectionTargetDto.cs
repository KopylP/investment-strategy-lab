using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionTargetDto(
    Guid InstrumentId,
    decimal Weight)
{
    public static SectionTargetDto From(SectionTarget sectionTarget) => new(
        InstrumentId: sectionTarget.InstrumentId,
        Weight: sectionTarget.Weight);
}
