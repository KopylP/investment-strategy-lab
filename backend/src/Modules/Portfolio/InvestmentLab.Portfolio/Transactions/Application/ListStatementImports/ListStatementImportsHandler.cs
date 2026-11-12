using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ListStatementImports;

public sealed class ListStatementImportsHandler(IStatementImportRepository imports) : IHandler
{
    public async Task<IReadOnlyList<StatementImportSummaryDto>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty)
            return [];

        var uploaded = await imports.ForAccountAsync(AccountId.Of(accountId), cancellationToken);

        return [.. uploaded.OrderByDescending(statement => statement.UploadedAt).Select(SummaryOf)];
    }

    private static StatementImportSummaryDto SummaryOf(StatementImport statement) => new(
        statement.Id,
        statement.Broker,
        statement.FileName,
        statement.UploadedAt,
        statement.Status.ToString(),
        statement.Mode.ToString(),
        statement.RecordedReferences.Count);
}
