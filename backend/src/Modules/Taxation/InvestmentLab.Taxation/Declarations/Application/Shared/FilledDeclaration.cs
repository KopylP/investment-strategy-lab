using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.Shared;

public sealed record FilledDeclaration(TaxDeclaration Declaration, IReadOnlyList<string> Warnings);
