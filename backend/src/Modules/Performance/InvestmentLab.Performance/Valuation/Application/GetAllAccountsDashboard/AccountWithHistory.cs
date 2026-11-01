using InvestmentLab.Performance.Valuation.Application.Shared;

namespace InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;

public sealed record AccountWithHistory(DashboardAccount Account, AccountHistory History);
