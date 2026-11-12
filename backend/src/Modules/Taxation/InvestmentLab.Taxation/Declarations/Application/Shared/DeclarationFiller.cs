using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.Shared;

public sealed class DeclarationFiller(IEnumerable<ITaxDeclarationForm> forms, DeclarationAccounts accounts, IInstrumentCatalog instruments)
{
    private const int CountryCodeLength = 2;

    public async Task<Result<FilledDeclaration>> FillAsync(int year, string code, CancellationToken cancellationToken)
    {
        if (forms.FirstOrDefault(form => string.Equals(form.Code, code, StringComparison.OrdinalIgnoreCase)) is not { } form)
            return Error.NotFound("tax.form_not_found", $"There is no form {code}.");

        var (declared, history) = await accounts.LoadAsync(year, cancellationToken);

        if (!DeclarationAccounts.HasTaxableYear(declared, form))
            return Error.NotFound("tax.declaration_empty", $"No taxable events for {form.Code} in {year}.");

        var mine = declared.Where(account => account.Policy.Jurisdiction == form.Jurisdiction).ToList();

        if (history.Incomplete.Count > 0)
            return Error.Validation("tax.declaration_incomplete", "The data is incomplete, the declaration is not filled.", new { details = history.Incomplete });

        var known = await instruments.GetInstruments(cancellationToken);
        var isins = known.ToDictionary(instrument => instrument.Id, instrument => instrument.Isin);

        var facts = new DeclarationFacts(year, mine, history.Losses, id => CountryOf(isins.GetValueOrDefault(id)));

        return new FilledDeclaration(form.Fill(facts), history.Warnings);
    }

    private static string? CountryOf(string? isin) =>
        isin is { Length: >= CountryCodeLength } ? isin[..CountryCodeLength].ToUpperInvariant() : null;
}
