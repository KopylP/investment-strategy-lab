using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationFactDto(
    string Label,
    decimal Amount)
{
    public static DeclarationFactDto From(DeclarationFact declarationFact) => new(
        Label: declarationFact.Label,
        Amount: declarationFact.Amount);
}
