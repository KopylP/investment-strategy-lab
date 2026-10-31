namespace InvestmentLab.Portfolio.Contracts.Transactions;

public sealed record TransactionRejectedV1(string SourceReference, string Code, string Reason);
