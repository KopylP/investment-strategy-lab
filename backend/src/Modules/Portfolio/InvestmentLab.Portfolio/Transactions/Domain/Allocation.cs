using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record Allocation(SubPortfolioId SubPortfolioId, Quantity Quantity);
