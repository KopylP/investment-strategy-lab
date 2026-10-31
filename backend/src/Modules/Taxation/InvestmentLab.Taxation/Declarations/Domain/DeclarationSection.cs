namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationSection(string Code, string Title, IReadOnlyList<DeclarationField> Fields);
