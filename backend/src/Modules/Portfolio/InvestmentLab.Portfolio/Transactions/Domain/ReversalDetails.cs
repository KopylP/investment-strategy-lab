namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record ReversalDetails(TransactionId Original, string Reason) : TransactionDetails;
