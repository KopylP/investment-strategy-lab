using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RemoveSignalReportRule;

public sealed class RemoveSignalReportRuleHandler(ISignalReportRuleRepository rules, TimeProvider clock)
{
    public async Task<Result> Handle(Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await rules.FindAsync(ruleId, cancellationToken);
        if (rule is null)
            return SignalReportRuleErrors.NotFound();

        var dismissed = rule.Dismiss(clock.GetUtcNow());
        if (dismissed.IsFailure)
            return dismissed;

        await rules.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
