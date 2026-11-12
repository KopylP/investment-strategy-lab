using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationSectionDto(
    string Code,
    string Title,
    IReadOnlyList<DeclarationFieldDto> Fields)
{
    public static DeclarationSectionDto From(DeclarationSection declarationSection) => new(
        Code: declarationSection.Code,
        Title: declarationSection.Title,
        Fields: [.. declarationSection.Fields.Select(DeclarationFieldDto.From)]);
}
