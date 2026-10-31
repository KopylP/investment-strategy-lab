namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationNote(string Text, bool Action = false, string? Short = null);
