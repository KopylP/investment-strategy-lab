using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionColumnDto(
    string Id,
    string Label,
    string Format,
    string? Hint,
    bool Compact,
    string? TrueHint,
    string? FalseHint)
{
    public static SectionColumnDto From(SectionColumn sectionColumn) => new(
        Id: sectionColumn.Id,
        Label: sectionColumn.Label,
        Format: sectionColumn.Format,
        Hint: sectionColumn.Hint,
        Compact: sectionColumn.Compact,
        TrueHint: sectionColumn.TrueHint,
        FalseHint: sectionColumn.FalseHint);
}
