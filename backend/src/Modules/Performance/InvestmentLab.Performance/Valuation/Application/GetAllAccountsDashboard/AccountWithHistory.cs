using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public sealed record AccountWithHistory(AccountSetupDto Account, AccountHistory History);
