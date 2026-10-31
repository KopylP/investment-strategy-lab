using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RemoveSignalReportRule;

public sealed class RemoveSignalReportRuleHandler(ISignalReportRuleRepository rules)
{
    public async Task<Result> Handle(Guid ruleId, CancellationToken cancellationToken)
    {
        var rule = await rules.FindAsync(ruleId, cancellationToken);
        if (rule is null)
            return SignalReportRuleErrors.NotFound();

        rules.Remove(rule);
        await rules.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
