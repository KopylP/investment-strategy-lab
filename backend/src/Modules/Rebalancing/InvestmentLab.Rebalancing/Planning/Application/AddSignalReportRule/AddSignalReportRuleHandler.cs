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

        var existing = await rules.FindAsync(command.SignalId, command.AccountId, command.ReportCode, cancellationToken);

        var restored = existing?.Restore() ?? Result.Success();
        if (restored.IsFailure)
            return restored.Error;

        var rule = existing ?? AddNew(ownerId, command);

        await rules.SaveChangesAsync(cancellationToken);

        return SignalReportRuleDto.Of(rule);
    }

    private SignalReportRule AddNew(UserId ownerId, AddSignalReportRuleCommand command)
    {
        var rule = SignalReportRule.Of(ownerId, command.SignalId, command.AccountId, command.ReportCode, clock.GetUtcNow());

        rules.Add(rule);

        return rule;
    }
}
