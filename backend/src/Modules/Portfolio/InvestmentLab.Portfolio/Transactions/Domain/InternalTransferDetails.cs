using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record InternalTransferDetails(SubPortfolioId From, SubPortfolioId To, Guid? InstrumentId, Quantity? Quantity, Money? Amount)
    : TransactionDetails;
