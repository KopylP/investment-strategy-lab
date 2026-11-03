namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class DeclaredSignalRules
{
    public static IReadOnlyList<DeclaredSignalRule> WantedBy(DeclaringAccount account, IReadOnlyList<BuiltInSignal> signals, IReadOnlyList<UserSignal>? userSignals = null)
    {
        if (!account.GeneratesReportsOnItsOwn)
            return [];

        var concerning = signals.Where(signal => signal.Concerns(account)).ToList();

        var own = (userSignals ?? []).Where(signal => signal.OwnerId == account.OwnerId).ToList();
        var builtIn = account.Reports.SelectMany(report => RulesOfReport(account, report, concerning));
        var user = account.Reports.SelectMany(report => UserRulesOfReport(account, report, own));

        return [.. builtIn.Concat(user).Distinct()];
    }

    public static SignalRuleChanges ChangesBetween(IReadOnlyList<DeclaredSignalRule> wanted, IReadOnlyList<SignalReportRule> existing)
    {
        var existingKeys = existing.Select(rule => (rule.SignalId, rule.AccountId, rule.ReportCode)).ToHashSet();
        var wantedKeys = wanted.Select(rule => (rule.SignalId, rule.AccountId, rule.ReportCode)).ToHashSet();

        var toAdd = wanted.Where(rule => !existingKeys.Contains((rule.SignalId, rule.AccountId, rule.ReportCode))).ToList();
        var toRemove = existing.Where(rule => IsNoLongerDeclared(rule, wantedKeys)).ToList();

        return new SignalRuleChanges(toAdd, toRemove);
    }

    private static IEnumerable<DeclaredSignalRule> RulesOfReport(DeclaringAccount account, AccountReport report, List<BuiltInSignal> signals) =>
        signals
            .Where(signal => report.ComesOn(signal.Kind))
            .Select(signal => new DeclaredSignalRule(account.OwnerId, signal.SignalId, account.Id, report.Code));

    private static IEnumerable<DeclaredSignalRule> UserRulesOfReport(DeclaringAccount account, AccountReport report, IReadOnlyList<UserSignal> signals) =>
        signals
            .Where(signal => report.SignalIds?.Contains(signal.SignalId) == true)
            .Select(signal => new DeclaredSignalRule(account.OwnerId, signal.SignalId, account.Id, report.Code));

    private static bool IsNoLongerDeclared(SignalReportRule rule, HashSet<(Guid, Guid, string)> wantedKeys) =>
        rule.FromStrategy && !rule.IsDismissed && !wantedKeys.Contains((rule.SignalId, rule.AccountId, rule.ReportCode));
}
