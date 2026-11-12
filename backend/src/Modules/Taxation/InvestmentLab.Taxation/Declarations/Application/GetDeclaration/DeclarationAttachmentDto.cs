using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record DeclarationAttachmentDto(
    string Form,
    string Title,
    IReadOnlyList<DeclarationFieldDto> Fields)
{
    public static DeclarationAttachmentDto From(DeclarationAttachment declarationAttachment) => new(
        Form: declarationAttachment.Form,
        Title: declarationAttachment.Title,
        Fields: [.. declarationAttachment.Fields.Select(DeclarationFieldDto.From)]);
}
