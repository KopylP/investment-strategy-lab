using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application;

public sealed record DeclarationFormDto(string Code, string Title);

public sealed record DeclarationDto(TaxDeclaration Declaration, IReadOnlyList<string> Warnings);

public sealed record Pit8CCommand(decimal Revenue, decimal Costs);

/// <summary>
/// The yearly returns of the signed-in user. A form takes every account whose tax policy is of its jurisdiction, so
/// it is offered only to people who have one; the person's taxes are recomputed first, and a return is never filled
/// from figures that miss something (an NBP rate, the purchase of sold units).
/// </summary>
public sealed class DeclarationQueries(
    IEnumerable<ITaxDeclarationForm> forms, IEnumerable<IDeclarationPrinter> printers, IAccountSetupQueries accounts, ITaxpayerHistory taxpayer,
    IPit8CRepository pit8C, IInstrumentCatalog instruments)
{
    public async Task<IReadOnlyList<DeclarationFormDto>> Available(int year, CancellationToken cancellationToken)
    {
        var (declared, _) = await AccountsAsync(year, cancellationToken);
        return [.. forms.Where(form => HasTaxableYear(declared, form)).Select(form => new DeclarationFormDto(form.Code, form.Title))];
    }

    public async Task<Result<DeclarationDto>> Fill(int year, string code, CancellationToken cancellationToken)
    {
        if (forms.FirstOrDefault(form => string.Equals(form.Code, code, StringComparison.OrdinalIgnoreCase)) is not { } form)
            return Error.NotFound("tax.form_not_found", $"There is no form {code}.");
        var (declared, history) = await AccountsAsync(year, cancellationToken);
        if (!HasTaxableYear(declared, form))
            return Error.NotFound("tax.declaration_empty", $"No taxable events for {form.Code} in {year}.");

        var mine = declared.Where(account => account.Policy.Jurisdiction == form.Jurisdiction).ToList();
        if (history.Incomplete.Count > 0)
            return Error.Validation("tax.declaration_incomplete", $"Дані неповні, декларацію не заповнено: {string.Join(" ", history.Incomplete)}");

        var isins = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id, instrument => instrument.Isin);
        // Losses from before the journal are the person's, kept once per person.
        var input = new DeclarationInput(year, mine, history.Losses,
            id => isins.GetValueOrDefault(id) is { Length: >= 2 } isin ? isin[..2].ToUpperInvariant() : null);
        return new DeclarationDto(form.Fill(input), history.Warnings);
    }

    /// <summary>The filled return as a PDF: a cheat sheet of what to type where ("summary"), or the official form ("form").</summary>
    public async Task<Result<(byte[] Content, string FileName)>> Pdf(int year, string code, string kind, CancellationToken cancellationToken)
    {
        var filled = await Fill(year, code, cancellationToken);
        if (filled.IsFailure)
            return filled.Error;
        var declaration = filled.Value.Declaration;
        if (printers.FirstOrDefault(printer => printer.Kind == kind && printer.Prints(declaration)) is not { } printer)
            return Error.NotFound("tax.form_not_printable", $"{declaration.Form} has no {kind} PDF.");
        return (printer.Print(declaration), $"{declaration.Form}-{year}-{(kind == "form" ? "formularz" : "sciaga")}.pdf");
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
    private static bool HasTaxableYear(IReadOnlyList<DeclarationAccount> declared, ITaxDeclarationForm form) =>
        declared.Any(account => account.Policy.Jurisdiction == form.Jurisdiction && account.Policy.IsTaxed && (account.Sales.Count > 0 || account.Dividends.Count > 0));

    /// <summary>
    /// Each taxed account's own years for the form's rows (revenue and costs per broker); netting, carried losses and
    /// the tax are the form's, over all of them.
    /// </summary>
    private async Task<(IReadOnlyList<DeclarationAccount> Accounts, TaxpayerHistoryDto History)> AccountsAsync(int year, CancellationToken cancellationToken)
    {
        var history = await taxpayer.LoadAsync(cancellationToken);
        var statements = (await pit8C.AllAsync(cancellationToken)).ToLookup(statement => statement.AccountId);
        var declared = history.Accounts.Select(account =>
        {
            var computation = TaxCalculator.Compute(account.Events, account.Policy, []);
            var ofYear = computation.Year(year);
            return new DeclarationAccount(account.Name, account.Broker, account.Policy, [.. computation.Years.Select(result => result.Summary)],
                ofYear?.Realizations ?? [], ofYear?.Dividends ?? [], account.Id, statements[account.Id].ToDictionary(statement => statement.Year, statement => statement.Amounts));
        }).ToList();
        return (declared, history);
    }
}
