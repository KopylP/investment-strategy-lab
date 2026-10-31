using InvestmentLab.Portfolio.Transactions.Application.ReverseRecordedTransaction;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.UndoStatementImport;

public sealed class UndoStatementImportHandler(
    StatementImportLoader loader,
    StatementPreview preview,
    IStatementImportRepository imports,
    ReverseRecordedTransactionHandler recordedReversals,
    TimeProvider clock)
{
    private const string AlreadyReversedCode = "transaction.already_reversed";

    public async Task<Result<StatementImportDto>> Handle(Guid accountId, Guid importId, CancellationToken cancellationToken)
    {
        var loaded = await loader.LoadAsync(accountId, importId, cancellationToken);
        if (loaded.IsFailure)
            return loaded.Error;

        var (account, statement) = loaded.Value;

        if (statement.Status != StatementImportStatus.Confirmed)
            return StatementImportErrors.NotConfirmed(statement.Id);

        await ReverseRecordedAsync(account.Id.Value, statement, cancellationToken);

        if (statement.ReversedTransactionIds.Count > 0)
            statement.Warnings.Add($"{statement.ReversedTransactionIds.Count} операцій, сторнованих під час імпорту, не відновлено: за потреби запишіть їх заново.");

        statement.Undo(clock.GetUtcNow());
        await imports.SaveChangesAsync(cancellationToken);

        return await preview.PlanAndShowAsync(account, statement, cancellationToken);
    }

    private async Task ReverseRecordedAsync(Guid accountId, StatementImport statement, CancellationToken cancellationToken)
    {
        foreach (var reference in Enumerable.Reverse(statement.RecordedReferences))
        {
            var result = await recordedReversals.Handle(accountId, reference, $"скасовано імпорт {statement.FileName}", cancellationToken);

            if (result.IsFailure && result.Error.Code != AlreadyReversedCode)
                statement.Warnings.Add($"{reference}: {result.Error.Message}");
        }
    }
}
