using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionDecisionDto(
    bool MarketIsFavorable,
    decimal InvestFraction,
    IReadOnlyList<string> MarketReasons,
    string Summary,
    IReadOnlyList<SectionTargetDto> Targets,
    IReadOnlyList<SectionScoreDto> Scores,
    IReadOnlyList<string> Warnings,
    string? ScoreTitle,
    string? InstrumentLabel,
    IReadOnlyList<SectionColumnDto>? Columns)
{
    public static SectionDecisionDto From(SectionDecision sectionDecision) => new(
        MarketIsFavorable: sectionDecision.MarketIsFavorable,
        InvestFraction: sectionDecision.InvestFraction,
        MarketReasons: sectionDecision.MarketReasons,
        Summary: sectionDecision.Summary,
        Targets: [.. sectionDecision.Targets.Select(SectionTargetDto.From)],
        Scores: [.. sectionDecision.Scores.Select(SectionScoreDto.From)],
        Warnings: sectionDecision.Warnings,
        ScoreTitle: sectionDecision.ScoreTitle,
        InstrumentLabel: sectionDecision.InstrumentLabel,
        Columns: sectionDecision.Columns?.Select(SectionColumnDto.From).ToList());
}
