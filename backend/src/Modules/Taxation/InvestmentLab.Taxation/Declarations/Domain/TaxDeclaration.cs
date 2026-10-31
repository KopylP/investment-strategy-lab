namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record TaxDeclaration(
    string Form,
    string Title,
    int Year,
    DateOnly Deadline,
    IReadOnlyList<DeclarationSection> Sections,
    IReadOnlyList<DeclarationAttachment> Attachments,
    IReadOnlyList<DeclarationAccountLine> Accounts,
    IReadOnlyList<DeclarationNote> Notes,
    decimal TaxDue = 0m,
    IReadOnlyList<DeclarationFact>? Facts = null);
