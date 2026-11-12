using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.GetDeclaration;

public sealed record TaxDeclarationDto(
    string Form,
    string Title,
    int Year,
    DateOnly Deadline,
    IReadOnlyList<DeclarationSectionDto> Sections,
    IReadOnlyList<DeclarationAttachmentDto> Attachments,
    IReadOnlyList<DeclarationAccountLineDto> Accounts,
    IReadOnlyList<DeclarationNoteDto> Notes,
    decimal TaxDue,
    IReadOnlyList<DeclarationFactDto>? Facts)
{
    public static TaxDeclarationDto From(TaxDeclaration taxDeclaration) => new(
        Form: taxDeclaration.Form,
        Title: taxDeclaration.Title,
        Year: taxDeclaration.Year,
        Deadline: taxDeclaration.Deadline,
        Sections: [.. taxDeclaration.Sections.Select(DeclarationSectionDto.From)],
        Attachments: [.. taxDeclaration.Attachments.Select(DeclarationAttachmentDto.From)],
        Accounts: [.. taxDeclaration.Accounts.Select(DeclarationAccountLineDto.From)],
        Notes: [.. taxDeclaration.Notes.Select(DeclarationNoteDto.From)],
        TaxDue: taxDeclaration.TaxDue,
        Facts: taxDeclaration.Facts?.Select(DeclarationFactDto.From).ToList());
}
