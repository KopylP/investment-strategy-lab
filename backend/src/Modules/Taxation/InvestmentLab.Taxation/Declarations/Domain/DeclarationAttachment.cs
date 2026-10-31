namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationAttachment(string Form, string Title, IReadOnlyList<DeclarationField> Fields);
