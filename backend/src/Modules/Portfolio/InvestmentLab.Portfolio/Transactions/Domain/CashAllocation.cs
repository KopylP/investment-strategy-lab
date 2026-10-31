using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record CashAllocation(SubPortfolioId SubPortfolioId, Money Amount);
