namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationDto(TaxDeclarationDto Declaration, IReadOnlyList<string> Warnings);
