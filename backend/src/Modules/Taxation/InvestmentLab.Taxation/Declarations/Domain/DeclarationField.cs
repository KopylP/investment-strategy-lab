namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationField(int Position, string Label, decimal? Amount, string? Text = null, string? Hint = null, bool Computed = false);
