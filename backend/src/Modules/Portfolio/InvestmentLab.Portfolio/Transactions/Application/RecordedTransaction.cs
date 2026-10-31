using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

public sealed record RecordedTransaction(Guid TransactionId, IReadOnlyList<string> Warnings);
