namespace InvestmentLab.Strategies.Contracts;

public sealed record RationaleColumnDto(string Id, string Label, string Format, string? Hint, bool Compact, string? TrueHint, string? FalseHint);
