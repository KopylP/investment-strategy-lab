namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TransactionRecorded(Transaction Transaction, Currency Currency, DateTimeOffset OccurredAt) : IDomainEvent;
