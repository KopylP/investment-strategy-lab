using InvestmentLab.Taxation.Declarations.Application.Shared;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.ListDeclarationForms;

public sealed class ListDeclarationFormsHandler(IEnumerable<ITaxDeclarationForm> forms, DeclarationAccounts accounts) : IHandler
{
    public async Task<IReadOnlyList<DeclarationFormDto>> Handle(int year, CancellationToken cancellationToken)
    {
        var (declared, _) = await accounts.LoadAsync(year, cancellationToken);

        return [.. forms.Where(form => DeclarationAccounts.HasTaxableYear(
            declared,
            form)).Select(form => new DeclarationFormDto(form.Code,
            form.Title))];
    }
}
