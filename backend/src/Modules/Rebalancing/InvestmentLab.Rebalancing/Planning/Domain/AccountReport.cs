using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record AccountReport(
    string Code,
    string Name,
    string Description,
    string Mode,
    IReadOnlyList<SubPortfolioReport> SubPortfolios,
    OwnReportSchedule? Own = null)
{
    public ReportKind Kind => Mode == StrategyReportDto.RiskReview ? ReportKind.RiskReview : ReportKind.Monthly;

    public Cadence CadenceOf(SubPortfolioReport sub) => Own?.Cadence ?? sub.Default;
}
