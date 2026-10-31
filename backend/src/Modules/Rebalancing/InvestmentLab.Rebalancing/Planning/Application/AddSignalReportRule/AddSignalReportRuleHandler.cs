using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.AddSignalReportRule;

public sealed record AddSignalReportRuleCommand(Guid SignalId, Guid AccountId, string ReportCode);

public sealed class AddSignalReportRuleHandler(ISignalReportRuleRepository rules, PlanningInputs inputs, TimeProvider clock)
{
    public async Task<Result<SignalReportRuleDto>> Handle(UserId ownerId, AddSignalReportRuleCommand command, CancellationToken cancellationToken)
    {
        var account = await inputs.AccountAsync(command.AccountId, cancellationToken);
        if (account is null)
            return SignalReportRuleErrors.AccountNotFound();

        var reports = await inputs.ReportsAsync(account, settings: null, cancellationToken);
        if (reports.All(report => report.Code != command.ReportCode))
            return SignalReportRuleErrors.ReportNotDeclared();

        if (await rules.ExistsAsync(command.SignalId, command.AccountId, command.ReportCode, cancellationToken))
            return SignalReportRuleErrors.AlreadyExists();

        var rule = SignalReportRule.Of(ownerId, command.SignalId, command.AccountId, command.ReportCode, clock.GetUtcNow());

        rules.Add(rule);
        await rules.SaveChangesAsync(cancellationToken);

        return SignalReportRuleDto.Of(rule);
    }
}
