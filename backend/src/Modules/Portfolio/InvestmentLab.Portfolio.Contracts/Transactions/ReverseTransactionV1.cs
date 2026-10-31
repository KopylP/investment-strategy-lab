namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record ReverseTransactionV1(Guid AccountId, string SourceReference, string Reason);
