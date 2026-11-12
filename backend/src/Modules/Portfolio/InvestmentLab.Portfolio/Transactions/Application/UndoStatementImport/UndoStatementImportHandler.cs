using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Transactions.Application.ReverseRecordedTransaction;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.UndoStatementImport;

public sealed class UndoStatementImportHandler(
    StatementImportLoader loader,
    StatementPreview preview,
    IStatementImportRepository imports,
    ReverseRecordedTransactionHandler recordedReversals,
    TimeProvider clock) : IHandler
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
            statement.Warnings.Add(Message.Of("statement.warning.reversals_not_restored", new { count = statement.ReversedTransactionIds.Count }));

        statement.Undo(clock.GetUtcNow());
        await imports.SaveChangesAsync(cancellationToken);

        return await preview.PlanAndShowAsync(account, statement, cancellationToken);
    }

    private async Task ReverseRecordedAsync(Guid accountId, StatementImport statement, CancellationToken cancellationToken)
    {
        foreach (var reference in Enumerable.Reverse(statement.RecordedReferences))
        {
            var result = await recordedReversals.Handle(accountId, reference, Message.Of("statement.reversal.undo", new { file = statement.FileName }), cancellationToken);

            if (result.IsFailure && result.Error.Code != AlreadyReversedCode)
                statement.Warnings.Add(Message.Of("statement.warning.undo_failed", new { reference, error = result.Error.AsMessage() }));
        }
    }
}
