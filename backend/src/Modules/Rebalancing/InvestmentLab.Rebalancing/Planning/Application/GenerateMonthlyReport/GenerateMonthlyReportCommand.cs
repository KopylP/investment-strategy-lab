namespace InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;

public sealed record GenerateMonthlyReportCommand(Guid AccountId, decimal? Contribution = null, DateOnly? AsOf = null, ReportScope? Scope = null);
