namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionDecision(
    bool MarketIsFavorable,
    decimal InvestFraction,
    IReadOnlyList<string> MarketReasons,
    string Summary,
    IReadOnlyList<SectionTarget> Targets,
    IReadOnlyList<SectionScore> Scores,
    IReadOnlyList<string> Warnings,
    string? ScoreTitle = null,
    string? InstrumentLabel = null,
    IReadOnlyList<SectionColumn>? Columns = null);
