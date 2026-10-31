using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed record SignalReportRuleDto(Guid Id, Guid SignalId, Guid AccountId, string ReportCode, string? Problem)
{
    public static SignalReportRuleDto Of(SignalReportRule rule) =>
        new(rule.Id, rule.SignalId, rule.AccountId, rule.ReportCode, rule.Problem);
}
