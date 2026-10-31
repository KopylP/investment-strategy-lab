using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record DeclarationDto(TaxDeclaration Declaration, IReadOnlyList<string> Warnings);
