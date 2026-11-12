using InvestmentLab.SharedKernel;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.GetTransactions;

public sealed record TransactionDto(
    Guid Id,
    string Kind,
    DateOnly OccurredOn,
    DateTimeOffset RecordedAt,
    string Source,
    string? SourceReference,
    bool IsReversed,
    TradeRecordedDto? Trade,
    CashMovementRecordedDto? Cash,
    InternalTransferRecordedDto? Transfer,
    Guid? ReversalOf,
    string? ReversalReason);

public sealed class GetTransactionsHandler(ITransactionJournal journal) : IHandler
{
    public async Task<Result<IReadOnlyList<TransactionDto>>> Handle(Guid accountId, CancellationToken cancellationToken)
    {
        if (accountId == Guid.Empty || await journal.FindAccountAsync(AccountId.Of(accountId), cancellationToken) is not { } account)
            return AccountErrors.NotFound(accountId);

        var entries = await journal.GetEntriesAsync(account.Id, cancellationToken);
        var reversed = entries.Where(entry => entry.ReversalOf is not null).Select(entry => entry.ReversalOf!.Value).ToHashSet();

        return Result<IReadOnlyList<TransactionDto>>.Success([.. entries
            .OrderByDescending(entry => entry.OccurredOn).ThenByDescending(entry => entry.RecordedAt)
            .Select(entry => new TransactionDto(
                entry.Id.Value,
                entry.Kind.ToString(),
                entry.OccurredOn,
                entry.RecordedAt,
                entry.Source.Kind.ToString(),
                entry.Source.Reference,
                reversed.Contains(entry.Id),
                (entry.Details as TradeDetails)?.ToDto(),
                (entry.Details as CashMovementDetails)?.ToDto(),
                (entry.Details as InternalTransferDetails)?.ToDto(),
                entry.ReversalOf?.Value,
                (entry.Details as ReversalDetails)?.Reason))]);
    }
}
