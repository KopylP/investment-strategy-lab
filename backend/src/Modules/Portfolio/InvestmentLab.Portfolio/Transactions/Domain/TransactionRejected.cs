namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record TransactionRejected(string SourceReference, string Code, string Reason, DateTimeOffset OccurredAt) : IDomainEvent;
