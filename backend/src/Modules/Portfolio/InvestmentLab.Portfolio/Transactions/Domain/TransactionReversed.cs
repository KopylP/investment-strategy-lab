namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TransactionReversed(Transaction Reversal, Transaction Original, DateTimeOffset OccurredAt) : IDomainEvent;
