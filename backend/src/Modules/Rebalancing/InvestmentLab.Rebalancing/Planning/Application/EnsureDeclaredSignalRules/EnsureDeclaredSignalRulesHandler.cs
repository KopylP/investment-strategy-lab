using InvestmentLab.Portfolio.Contracts.Accounts;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Signals.Contracts;

namespace InvestmentLab.Rebalancing.Planning.Application.EnsureDeclaredSignalRules;

public sealed class EnsureDeclaredSignalRulesHandler(
    PlanningInputs inputs,
    IPlanSettingsRepository settings,
    IBuiltInSignalQueries builtInSignals,
    ISignalReportRuleRepository rules,
    TimeProvider clock)
{
    public async Task Handle(CancellationToken cancellationToken)
    {
        var signals = await BuiltInSignalsAsync(cancellationToken);
        var wanted = await WantedRulesAsync(signals, cancellationToken);
        var existing = await rules.AllWithDismissedAsync(cancellationToken);

        var changes = DeclaredSignalRules.ChangesBetween(wanted, existing);
        if (changes.ToAdd.Count == 0 && changes.ToRemove.Count == 0)
            return;

        Apply(changes);

        await rules.SaveChangesAsync(cancellationToken);
    }

    private async Task<List<BuiltInSignal>> BuiltInSignalsAsync(CancellationToken cancellationToken)
    {
        var known = await builtInSignals.GetBuiltInSignals(cancellationToken);

        return [.. known.Select(BuiltInSignalOf)];
    }

    private static BuiltInSignal BuiltInSignalOf(BuiltInSignalDto signal) => new(
        SignalId: signal.SignalId,
        OwnerId: UserId.Of(signal.OwnerId),
        AccountId: signal.AccountId,
        Kind: signal.Kind);

    private async Task<List<DeclaredSignalRule>> WantedRulesAsync(List<BuiltInSignal> signals, CancellationToken cancellationToken)
    {
        var wanted = new List<DeclaredSignalRule>();

        foreach (var account in await inputs.AccountsAsync(cancellationToken))
        {
            var declaring = await DeclaringAccountOfAsync(account, cancellationToken);
            wanted.AddRange(DeclaredSignalRules.WantedBy(declaring, signals));
        }

        return wanted;
    }

    private async Task<DeclaringAccount> DeclaringAccountOfAsync(AccountSetupDto account, CancellationToken cancellationToken)
    {
        var plan = await settings.FindAsync(account.Id, cancellationToken);
        var reports = await inputs.ReportsAsync(account, plan, cancellationToken);

        return new DeclaringAccount(
            Id: account.Id,
            OwnerId: UserId.Of(account.OwnerId),
            GeneratesReportsOnItsOwn: plan is not { GenerateOnSchedule: false },
            Reports: reports);
    }

    private void Apply(SignalRuleChanges changes)
    {
        var now = clock.GetUtcNow();

        foreach (var declared in changes.ToAdd)
            rules.Add(SignalReportRule.Declared(declared, now));

        foreach (var rule in changes.ToRemove)
            rules.Remove(rule);
    }
}
