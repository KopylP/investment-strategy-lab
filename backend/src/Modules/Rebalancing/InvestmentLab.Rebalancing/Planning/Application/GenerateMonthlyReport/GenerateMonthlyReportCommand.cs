using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed record GenerateMonthlyReportCommand(Guid AccountId, ReportScope Scope, decimal? Contribution = null, DateOnly? AsOf = null);
