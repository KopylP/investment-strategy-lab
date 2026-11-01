using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Tests.Accounts;

public sealed class AccountRequestRulesTests
{
    [Fact]
    public void A_new_sub_portfolio_becomes_a_draft_with_its_percents_as_fractions()
    {
        var strategyId = Guid.NewGuid();
        var asked = new NewSubPortfolio("EU", 70m, strategyId, 3, MaxPositionWeightPercent: 20m, MaxMonthlyTurnoverPercent: 50m);

        var draft = SubPortfolioDraft.TryOf(asked).Value;

        draft.Name.ShouldBe("EU");
        draft.TargetShare.Value.Value.ShouldBe(0.7m);
        draft.Strategy.ShouldNotBeNull();
        draft.Guardrails.MaxPositionWeight.ShouldBe(Percentage.Of(0.2m));
        draft.Guardrails.MaxMonthlyTurnover.ShouldBe(Percentage.Of(0.5m));
    }

    [Fact]
    public void A_new_sub_portfolio_without_a_strategy_or_limits_has_none()
    {
        var draft = SubPortfolioDraft.TryOf(new NewSubPortfolio("EU", 100m)).Value;

        draft.Strategy.ShouldBeNull();
        draft.Guardrails.MaxPositionWeight.ShouldBeNull();
        draft.Guardrails.MaxMonthlyTurnover.ShouldBeNull();
    }

    [Theory]
    [InlineData(150, null)]
    [InlineData(50, 120)]
    public void A_share_or_a_limit_out_of_range_is_refused(int share, int? maxWeight)
    {
        SubPortfolioDraft.TryOf(new NewSubPortfolio("EU", share, MaxPositionWeightPercent: maxWeight)).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void Target_shares_are_read_as_percents_and_no_shares_is_an_empty_set()
    {
        var id = Guid.NewGuid();

        var shares = TargetShares.TryFromPercents(new Dictionary<Guid, decimal> { [id] = 40m }).Value;

        shares[SubPortfolioId.Of(id)].Value.Value.ShouldBe(0.4m);
        TargetShares.TryFromPercents(null).Value.ShouldBeEmpty();
    }

    [Fact]
    public void A_target_share_without_a_sub_portfolio_or_out_of_range_is_refused()
    {
        TargetShares.TryFromPercents(new Dictionary<Guid, decimal> { [Guid.Empty] = 40m }).Error.ShouldBe(AccountErrors.TargetSharesIncomplete());
        TargetShares.TryFromPercents(new Dictionary<Guid, decimal> { [Guid.NewGuid()] = 140m }).IsFailure.ShouldBeTrue();
    }

    [Fact]
    public void A_contribution_policy_is_chosen_by_name_in_any_case_with_the_default_band_unless_given()
    {
        ContributionPolicy.TryOf("fixedsplit", 10m).Value.ShouldBe(new ContributionPolicy(ContributionPolicyKind.FixedSplit, Percentage.Of(0.1m)));
        ContributionPolicy.TryOf("FillUnderweight", null).Value.DriftBand.ShouldBe(ContributionPolicy.DefaultDriftBand);
    }

    [Theory]
    [InlineData("Unknown", 5)]
    [InlineData("FixedSplit", 500)]
    public void An_unknown_policy_or_a_band_out_of_range_is_refused(string kind, int band)
    {
        ContributionPolicy.TryOf(kind, band).IsFailure.ShouldBeTrue();
    }
}
