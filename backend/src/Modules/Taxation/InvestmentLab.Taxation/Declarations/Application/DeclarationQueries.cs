using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record DeclarationFormDto(string Code, string Title);

public sealed record DeclarationDto(TaxDeclaration Declaration, IReadOnlyList<string> Warnings);

/// <summary>
/// The yearly returns of the signed-in user. A form takes every account whose tax policy is of its jurisdiction, so
/// it is offered only to people who have one; each account is recomputed first.
/// </summary>
public sealed class DeclarationQueries(
    IEnumerable<ITaxDeclarationForm> forms, IEnumerable<IDeclarationPrinter> printers, IAccountSetupQueries accounts, TaxRecalculator recalculator, IAssessmentRepository assessments, IInstrumentCatalog instruments)
{
    public async Task<IReadOnlyList<DeclarationFormDto>> Available(int year, CancellationToken cancellationToken)
    {
        var declared = await AccountsAsync(year, cancellationToken);
        return [.. forms.Where(form => HasTaxableYear(declared, form, year)).Select(form => new DeclarationFormDto(form.Code, form.Title))];
    }

    public async Task<Result<DeclarationDto>> Fill(int year, string code, CancellationToken cancellationToken)
    {
        if (forms.FirstOrDefault(form => string.Equals(form.Code, code, StringComparison.OrdinalIgnoreCase)) is not { } form)
            return Error.NotFound("tax.form_not_found", $"There is no form {code}.");
        var declared = await AccountsAsync(year, cancellationToken);
        if (!HasTaxableYear(declared, form, year))
            return Error.NotFound("tax.declaration_empty", $"No taxable events for {form.Code} in {year}.");

        var mine = declared.Where(account => account.Account.Policy.Jurisdiction == form.Jurisdiction).ToList();
        // A loss from before the journal is the person's, entered on any account: the same one entered twice counts once.
        var losses = mine.Where(account => account.Account.Policy.IsTaxed).SelectMany(account => account.Losses).Distinct().ToList();
        var isins = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id, instrument => instrument.Isin);
        var input = new DeclarationInput(year, [.. mine.Select(account => account.Account)], losses,
            id => isins.GetValueOrDefault(id) is { Length: >= 2 } isin ? isin[..2].ToUpperInvariant() : null);
        return new DeclarationDto(form.Fill(input), [.. mine.SelectMany(account => account.Warnings)]);
    }

    /// <summary>The filled return on its official form, as a PDF.</summary>
    public async Task<Result<(byte[] Content, string FileName)>> Pdf(int year, string code, CancellationToken cancellationToken)
    {
        var filled = await Fill(year, code, cancellationToken);
        if (filled.IsFailure)
            return filled.Error;
        var declaration = filled.Value.Declaration;
        if (printers.FirstOrDefault(printer => printer.Form == declaration.Form) is not { } printer)
            return Error.NotFound("tax.form_not_printable", $"{declaration.Form} has no PDF.");
        return (printer.Print(declaration), $"{declaration.Form}-{year}.pdf");
    }

    private static bool HasTaxableYear(IReadOnlyList<Declared> declared, ITaxDeclarationForm form, int year) =>
        declared.Any(account => account.Account.Policy.Jurisdiction == form.Jurisdiction && account.Account.Policy.IsTaxed && account.Account.In(year) is not null);

    private async Task<IReadOnlyList<Declared>> AccountsAsync(int year, CancellationToken cancellationToken)
    {
        var declared = new List<Declared>();
        foreach (var setup in (await accounts.GetAccountSetups(cancellationToken)).OrderBy(setup => setup.Name))
        {
            var outcome = await recalculator.RecomputeAsync(setup.Id, cancellationToken);
            if (outcome.IsFailure)
                continue;
            var assessed = await assessments.ForAccountAsync(setup.Id, cancellationToken);
            var ofYear = assessed.FirstOrDefault(assessment => assessment.Year == year);
            var account = new DeclarationAccount(setup.Name, setup.Broker, TaxPolicies.For(setup.TaxPolicyCode), [.. assessed.Select(assessment => assessment.Summary)],
                ofYear?.Realizations ?? [], ofYear?.Dividends ?? []);
            declared.Add(new Declared(account, (await assessments.LossesAsync(setup.Id, cancellationToken)).Select(loss => loss.ToLossYear()).ToList(),
                [.. outcome.Value.Warnings.Select(warning => $"{setup.Name}: {warning}")]));
        }
        return declared;
    }

    private sealed record Declared(DeclarationAccount Account, IReadOnlyList<LossYear> Losses, IReadOnlyList<string> Warnings);
}
