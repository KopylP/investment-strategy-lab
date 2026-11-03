using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Tests.Planning;

public sealed class DeclaredSignalRulesTests
{
    private const string Drawdown = "position-drawdown";
    private const string RiskReview = "risk-review";

    private static readonly UserId Owner = UserId.Of(Guid.NewGuid());
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AccountId = Guid.NewGuid();
    private static readonly Guid SignalId = Guid.NewGuid();

    private static AccountReport Report(params string[] signalKinds) =>
        new(RiskReview, RiskReview, "", "riskReview", ReportKind.RiskReview, [], SignalKinds: signalKinds);

    private static DeclaringAccount Account(bool generates = true, params AccountReport[] reports) =>
        new(AccountId, Owner, generates, reports);

    private static BuiltInSignal Signal(Guid? accountId, string kind = Drawdown, UserId? owner = null) =>
        new(SignalId, owner ?? Owner, accountId, kind);

    private static DeclaredSignalRule Wanted() => new(Owner, SignalId, AccountId, RiskReview);

    [Fact]
    public void A_report_that_comes_on_a_kind_wants_a_rule_on_the_signal_of_its_account()
    {
        var wanted = DeclaredSignalRules.WantedBy(Account(true, Report(Drawdown)), [Signal(AccountId)]);

        wanted.ShouldBe([Wanted()]);
    }

    [Fact]
    public void A_signal_without_an_account_serves_every_account_of_its_owner()
    {
        var wanted = DeclaredSignalRules.WantedBy(Account(true, Report(Drawdown)), [Signal(accountId: null)]);

        wanted.ShouldBe([Wanted()]);
    }

    [Fact]
    public void A_signal_of_another_account_owner_or_kind_wants_no_rule()
    {
        BuiltInSignal[] signals = [Signal(Guid.NewGuid()), Signal(AccountId, owner: UserId.Of(Guid.NewGuid())), Signal(AccountId, kind: "other")];

        DeclaredSignalRules.WantedBy(Account(true, Report(Drawdown)), signals).ShouldBeEmpty();
    }

    [Fact]
    public void A_report_that_declares_no_kinds_wants_no_rule()
    {
        var report = new AccountReport(RiskReview, RiskReview, "", "riskReview", ReportKind.RiskReview, []);

        DeclaredSignalRules.WantedBy(Account(true, report), [Signal(AccountId)]).ShouldBeEmpty();
    }

    [Fact]
    public void An_account_that_does_not_generate_reports_on_its_own_wants_no_rule()
    {
        DeclaredSignalRules.WantedBy(Account(false, Report(Drawdown)), [Signal(AccountId)]).ShouldBeEmpty();
    }

    [Fact]
    public void A_wanted_rule_that_does_not_exist_is_added()
    {
        var changes = DeclaredSignalRules.ChangesBetween([Wanted()], []);

        changes.ToAdd.ShouldBe([Wanted()]);
        changes.ToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void A_rule_the_user_made_by_hand_is_not_duplicated_and_stays_when_nothing_wants_it()
    {
        var byHand = SignalReportRule.Of(Owner, SignalId, AccountId, RiskReview, Now);

        DeclaredSignalRules.ChangesBetween([Wanted()], [byHand]).ToAdd.ShouldBeEmpty();
        DeclaredSignalRules.ChangesBetween([], [byHand]).ToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void A_rule_the_strategy_no_longer_declares_is_removed()
    {
        var declared = SignalReportRule.Declared(Wanted(), Now);

        DeclaredSignalRules.ChangesBetween([], [declared]).ToRemove.ShouldBe([declared]);
    }

    [Fact]
    public void A_rule_the_user_dismissed_does_not_come_back_and_is_remembered()
    {
        var dismissed = SignalReportRule.Declared(Wanted(), Now);
        dismissed.Dismiss(Now);

        DeclaredSignalRules.ChangesBetween([Wanted()], [dismissed]).ToAdd.ShouldBeEmpty();
        DeclaredSignalRules.ChangesBetween([], [dismissed]).ToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void A_rule_made_by_hand_and_removed_does_not_come_back_as_declared()
    {
        var byHand = SignalReportRule.Of(Owner, SignalId, AccountId, RiskReview, Now);

        byHand.Dismiss(Now).IsSuccess.ShouldBeTrue();

        byHand.IsDismissed.ShouldBeTrue();
        DeclaredSignalRules.ChangesBetween([Wanted()], [byHand]).ToAdd.ShouldBeEmpty();
    }

    [Fact]
    public void A_rule_added_again_belongs_to_the_user_and_stays_when_nothing_declares_it()
    {
        var rule = SignalReportRule.Declared(Wanted(), Now);
        rule.CouldNotAct("problem");
        rule.Dismiss(Now);

        rule.Restore().IsSuccess.ShouldBeTrue();

        rule.IsDismissed.ShouldBeFalse();
        rule.FromStrategy.ShouldBeFalse();
        rule.Problem.ShouldBeNull();
        DeclaredSignalRules.ChangesBetween([], [rule]).ToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void A_rule_already_in_force_cannot_be_restored()
    {
        var rule = SignalReportRule.Declared(Wanted(), Now);

        rule.Restore().Error.ShouldBe(SignalReportRuleErrors.AlreadyExists());
    }

    [Fact]
    public void A_rule_already_dismissed_cannot_be_removed_again()
    {
        var rule = SignalReportRule.Declared(Wanted(), Now);
        rule.Dismiss(Now);

        rule.Dismiss(Now.AddDays(1)).Error.ShouldBe(SignalReportRuleErrors.NotFound());
        rule.DismissedAt.ShouldBe(Now);
    }
    [Fact]
    public void A_report_that_names_a_user_signal_wants_a_rule_for_its_owner()
    {
        var report = Report() with { SignalIds = [SignalId] };

        DeclaredSignalRules.WantedBy(Account(true, report), [], [new UserSignal(SignalId, Owner)]).ShouldBe([Wanted()]);
    }

    [Fact]
    public void A_deleted_user_signal_gets_no_rule()
    {
        var report = Report() with { SignalIds = [SignalId] };

        DeclaredSignalRules.WantedBy(Account(true, report), [], []).ShouldBeEmpty();
    }

    [Fact]
    public void A_user_signal_of_someone_else_gets_no_rule()
    {
        var report = Report() with { SignalIds = [SignalId] };

        DeclaredSignalRules.WantedBy(Account(true, report), [], [new UserSignal(SignalId, UserId.New())]).ShouldBeEmpty();
    }

}
