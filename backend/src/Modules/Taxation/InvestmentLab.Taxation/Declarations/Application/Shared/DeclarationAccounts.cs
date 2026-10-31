using InvestmentLab.Taxation.Assessments.Domain;
using InvestmentLab.Taxation.Declarations.Domain;

namespace InvestmentLab.Taxation.Declarations.Application.Shared;

public sealed class DeclarationAccounts(ITaxpayerHistory taxpayer, IPit8CRepository pit8C)
{
    public static bool HasTaxableYear(IReadOnlyList<DeclarationAccount> declared, ITaxDeclarationForm form) =>
        declared.Any(account => account.Policy.Jurisdiction == form.Jurisdiction
            && account.Policy.IsTaxed
            && (account.Sales.Count > 0 || account.Dividends.Count > 0));

    public async Task<(IReadOnlyList<DeclarationAccount> Accounts, TaxpayerHistoryDto History)> LoadAsync(
        int year,
        CancellationToken cancellationToken)
    {
        var history = await taxpayer.LoadAsync(cancellationToken);
        var statements = (await pit8C.AllAsync(cancellationToken)).ToLookup(statement => statement.AccountId);
        var declared = history.Accounts.Select(account => DeclarationAccountOf(account, year, statements)).ToList();

        return (declared, history);
    }

    private static DeclarationAccount DeclarationAccountOf(TaxpayerAccountHistory account, int year, ILookup<Guid, Pit8CStatement> statements)
    {
        var computation = TaxCalculator.Compute(account.Events, account.Policy, []);
        var ofYear = computation.Year(year);

        return new DeclarationAccount(
            account.Name,
            account.Broker,
            account.Policy,
            [.. computation.Years.Select(result => result.Summary)],
            ofYear?.Realizations ?? [],
            ofYear?.Dividends ?? [],
            account.Id,
            statements[account.Id].ToDictionary(statement => statement.Year, statement => statement.Amounts));
    }
}
