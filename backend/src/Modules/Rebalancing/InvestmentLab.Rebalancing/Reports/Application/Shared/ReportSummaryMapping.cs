using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Shared;

internal static class ReportSummaryMapping
{
    public static ReportSummaryDto ToSummaryDto(this RebalancingReport report) => new(
        Id: report.Id.Value,
        AccountId: report.AccountId,
        AccountName: report.AccountName,
        Period: report.Period.ToString(),
        Kind: report.Kind.ToString(),
        Code: report.Code,
        Title: report.Title,
        Trigger: report.Trigger.ToString(),
        Status: report.Status.ToString(),
        MarketAsOf: report.MarketAsOf,
        GeneratedAt: report.GeneratedAt,
        ClosedAt: report.ClosedAt,
        Currency: report.Currency.Code,
        Contribution: report.Contribution.Total,
        ActionsTotal: report.Actions.Count,
        ActionsOpen: report.Actions.Count(action => !action.IsResolved),
        SupersededBy: report.SupersededBy?.Value);
}
