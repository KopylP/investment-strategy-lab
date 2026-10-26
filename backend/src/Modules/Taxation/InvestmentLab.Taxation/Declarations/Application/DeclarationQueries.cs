using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Application;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record DeclarationFormDto(string Code, string Title);

public sealed record DeclarationDto(TaxDeclaration Declaration, IReadOnlyList<string> Warnings);

public sealed record Pit8CCommand(decimal Revenue, decimal Costs);

/// <summary>
/// The yearly returns of the signed-in user. A form takes every account whose tax policy is of its jurisdiction, so
/// it is offered only to people who have one; each account is recomputed first, and a return is never filled from
/// figures that miss something (an NBP rate, the purchase of sold units).
/// </summary>
public sealed class DeclarationQueries(
    IEnumerable<ITaxDeclarationForm> forms, IEnumerable<IDeclarationPrinter> printers, IAccountSetupQueries accounts, TaxRecalculator recalculator,
    IAssessmentRepository assessments, IPit8CRepository pit8C, IInstrumentCatalog instruments)
{
    public async Task<IReadOnlyList<DeclarationFormDto>> Available(int year, CancellationToken cancellationToken)
    {
        var declared = await AccountsAsync(year, cancellationToken);
        return [.. forms.Where(form => HasTaxableYear(declared, form)).Select(form => new DeclarationFormDto(form.Code, form.Title))];
    }

    public async Task<Result<DeclarationDto>> Fill(int year, string code, CancellationToken cancellationToken)
    {
        if (forms.FirstOrDefault(form => string.Equals(form.Code, code, StringComparison.OrdinalIgnoreCase)) is not { } form)
            return Error.NotFound("tax.form_not_found", $"There is no form {code}.");
        var declared = await AccountsAsync(year, cancellationToken);
        if (!HasTaxableYear(declared, form))
            return Error.NotFound("tax.declaration_empty", $"No taxable events for {form.Code} in {year}.");

        var mine = declared.Where(account => account.Account.Policy.Jurisdiction == form.Jurisdiction).ToList();
        var incomplete = mine.Where(account => account.Account.Policy.IsTaxed).SelectMany(account => account.Incomplete).ToList();
        if (incomplete.Count > 0)
            return Error.Validation("tax.declaration_incomplete", $"Дані неповні, декларацію не заповнено: {string.Join(" ", incomplete)}");

        // A loss from before the journal is the person's, entered on any account: one per year. Two different ones
        // for the same year are a mistake to fix, not to add up.
        var losses = new List<LossYear>();
        foreach (var ofYear in mine.Where(account => account.Account.Policy.IsTaxed).SelectMany(account => account.Losses).GroupBy(loss => loss.Year))
        {
            if (ofYear.Distinct().Count() > 1)
                return Error.Validation("tax.losses_conflict", $"Збиток {ofYear.Key} року введено на кількох рахунках різними сумами — залиште один.");
            losses.Add(ofYear.First());
        }

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

    /// <summary>Records (or corrects) what a broker's PIT-8C says of an account's year.</summary>
    public async Task<Result> SetPit8C(int year, Guid accountId, Pit8CCommand command, CancellationToken cancellationToken)
    {
        if (await accounts.GetAccountSetup(accountId, cancellationToken) is not { } setup)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");
        if ((await pit8C.ForYearAsync(year, cancellationToken)).FirstOrDefault(statement => statement.AccountId == accountId) is { } existing)
        {
            var changed = existing.Change(command.Revenue, command.Costs);
            if (changed.IsFailure)
                return changed.Error;
        }
        else
        {
            var recorded = Pit8CStatement.Record(accountId, UserId.Of(setup.OwnerId), year, command.Revenue, command.Costs);
            if (recorded.IsFailure)
                return recorded.Error;
            pit8C.Add(recorded.Value);
        }
        await pit8C.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemovePit8C(int year, Guid accountId, CancellationToken cancellationToken)
    {
        if ((await pit8C.ForYearAsync(year, cancellationToken)).FirstOrDefault(statement => statement.AccountId == accountId) is not { } statement)
            return Error.NotFound("tax.pit8c_not_found", $"No PIT-8C amounts for {year}.");
        pit8C.Remove(statement);
        await pit8C.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    // A year in which the person sold or was paid something: years between such events hold no return.
    private static bool HasTaxableYear(IReadOnlyList<Declared> declared, ITaxDeclarationForm form) =>
        declared.Any(account => account.Account.Policy.Jurisdiction == form.Jurisdiction && account.Account.Policy.IsTaxed
            && (account.Account.Sales.Count > 0 || account.Account.Dividends.Count > 0));

    private async Task<IReadOnlyList<Declared>> AccountsAsync(int year, CancellationToken cancellationToken)
    {
        var declared = new List<Declared>();
        var statements = (await pit8C.ForYearAsync(year, cancellationToken)).ToDictionary(statement => statement.AccountId);
        foreach (var setup in (await accounts.GetAccountSetups(cancellationToken)).OrderBy(setup => setup.Name))
        {
            // An account without a journal yet has nothing to declare.
            var outcome = await recalculator.RecomputeAsync(setup.Id, cancellationToken);
            if (outcome.IsFailure)
                continue;
            var assessed = await assessments.ForAccountAsync(setup.Id, cancellationToken);
            var ofYear = assessed.FirstOrDefault(assessment => assessment.Year == year);
            var account = new DeclarationAccount(setup.Name, setup.Broker, TaxPolicies.For(setup.TaxPolicyCode), [.. assessed.Select(assessment => assessment.Summary)],
                ofYear?.Realizations ?? [], ofYear?.Dividends ?? [], setup.Id, statements.GetValueOrDefault(setup.Id)?.Amounts);
            declared.Add(new Declared(account, (await assessments.LossesAsync(setup.Id, cancellationToken)).Select(loss => loss.ToLossYear()).ToList(),
                [.. outcome.Value.Warnings.Select(warning => $"{setup.Name}: {warning}")],
                [.. outcome.Value.Incomplete.Select(problem => $"{setup.Name}: {problem}")]));
        }
        return declared;
    }

    private sealed record Declared(DeclarationAccount Account, IReadOnlyList<LossYear> Losses, IReadOnlyList<string> Warnings, IReadOnlyList<string> Incomplete);
}
