namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed record RecordedTransaction(Guid TransactionId, IReadOnlyList<string> Warnings);
