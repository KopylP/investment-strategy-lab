using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationFieldDto(
    int Position,
    string Label,
    decimal? Amount,
    string? Text,
    string? Hint,
    bool Computed)
{
    public static DeclarationFieldDto From(DeclarationField declarationField) => new(
        Position: declarationField.Position,
        Label: declarationField.Label,
        Amount: declarationField.Amount,
        Text: declarationField.Text,
        Hint: declarationField.Hint,
        Computed: declarationField.Computed);
}
