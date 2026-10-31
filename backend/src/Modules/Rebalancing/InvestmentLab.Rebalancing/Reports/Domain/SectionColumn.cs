namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed record SectionColumn(string Id, string Label, string Format, string? Hint, bool Compact, string? TrueHint, string? FalseHint);
