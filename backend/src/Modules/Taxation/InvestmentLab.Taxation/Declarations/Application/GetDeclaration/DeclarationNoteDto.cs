using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationNoteDto(
    string Text,
    bool Action,
    string? Short)
{
    public static DeclarationNoteDto From(DeclarationNote declarationNote) => new(
        Text: declarationNote.Text,
        Action: declarationNote.Action,
        Short: declarationNote.Short);
}
