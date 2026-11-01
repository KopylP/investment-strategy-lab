using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Tests.Accounts;

public sealed class InvestmentAccountTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);

    private static SubPortfolioDraft Draft(string name, decimal percent) =>
        new(name, TargetShare.TryFromPercent(percent).Value, null, null, Guardrails.None);

    private static Result<InvestmentAccount> Open(params SubPortfolioDraft[] drafts) =>
        InvestmentAccount.Open(UserId.New(), "XTB", "XTB", Currency.EUR, "TEST-POLICY", ContributionPolicy.Default, drafts, Now);

    [Fact]
    public void Renames_a_sub_portfolio_and_allows_changing_only_the_case_of_its_own_name()
    {
        var account = Open(Draft("EU momentum", 60), Draft("US momentum", 40)).Value;
        var renamed = account.ActiveSubPortfolios.First();

        account.RenameSubPortfolio(renamed.Id, "  EU MOMENTUM ").IsSuccess.ShouldBeTrue();

        renamed.Name.ShouldBe("EU MOMENTUM");
    }

    [Theory]
    [InlineData("US momentum", "account.sub_portfolio_name_taken")]
    [InlineData("  ", "account.name_required")]
    public void Refuses_a_name_taken_by_another_sub_portfolio_or_blank(string name, string code)
    {
        var account = Open(Draft("EU momentum", 60), Draft("US momentum", 40)).Value;

        var renamed = account.RenameSubPortfolio(account.ActiveSubPortfolios.First().Id, name);

        renamed.Error.Code.ShouldBe(code);
    }

    [Fact]
    public void Opens_when_target_shares_add_up_to_100_percent()
    {
        var account = Open(Draft("EU momentum", 60), Draft("US momentum", 40)).Value;

        account.ActiveSubPortfolios.Select(sub => sub.TargetShare.Value.Value).ShouldBe([0.6m, 0.4m]);
        account.TaxPolicyCode.ShouldBe("TEST-POLICY");
    }

    [Theory]
    [InlineData(60, 30)]
    [InlineData(60, 50)]
    public void Refuses_shares_that_do_not_add_up(decimal first, decimal second) =>
        Open(Draft("A", first), Draft("B", second)).Error.Code.ShouldBe("account.target_shares_not_100");

    [Fact]
    public void Refuses_an_account_without_sub_portfolios() =>
        Open().Error.Code.ShouldBe("account.no_sub_portfolios");

    [Fact]
    public void Refuses_two_active_sub_portfolios_with_the_same_name() =>
        Open(Draft("Core", 50), Draft("core", 50)).Error.Code.ShouldBe("account.sub_portfolio_name_taken");

    [Fact]
    public void Refuses_a_base_currency_other_than_eur_or_usd() =>
        InvestmentAccount.Open(UserId.New(), "A", "XTB", Currency.PLN, "TEST-POLICY", ContributionPolicy.Default, [Draft("A", 100)], Now)
            .Error.Code.ShouldBe("account.base_currency_unsupported");

    [Fact]
    public void Adding_a_sub_portfolio_rebalances_the_others_in_one_step()
    {
        var account = Open(Draft("EU", 100)).Value;
        var eu = account.SubPortfolios[0].Id;

        var added = account.AddSubPortfolio(Draft("US", 30), new Dictionary<SubPortfolioId, TargetShare> { [eu] = TargetShare.TryFromPercent(70).Value }, Now);

        added.IsSuccess.ShouldBeTrue();
        account.FindActive(eu)!.TargetShare.Value.Value.ShouldBe(0.7m);
        account.FindActive(added.Value)!.TargetShare.Value.Value.ShouldBe(0.3m);
    }

    [Fact]
    public void A_failed_add_leaves_the_account_unchanged()
    {
        var account = Open(Draft("EU", 100)).Value;
        var eu = account.SubPortfolios[0].Id;

        var added = account.AddSubPortfolio(Draft("US", 30), new Dictionary<SubPortfolioId, TargetShare> { [eu] = TargetShare.TryFromPercent(100).Value }, Now);

        added.Error.Code.ShouldBe("account.target_shares_not_100");
        account.SubPortfolios.Count.ShouldBe(1);
        account.FindActive(eu)!.TargetShare.Value.Value.ShouldBe(1m);
    }

    [Fact]
    public void Changing_shares_needs_every_active_sub_portfolio()
    {
        var account = Open(Draft("A", 50), Draft("B", 50)).Value;

        account.ChangeTargetShares(new Dictionary<SubPortfolioId, TargetShare> { [account.SubPortfolios[0].Id] = TargetShare.TryFromPercent(100).Value }, Now)
            .Error.Code.ShouldBe("account.target_shares_incomplete");
    }

    [Fact]
    public void Retiring_hands_the_share_to_the_remaining_sub_portfolios()
    {
        var account = Open(Draft("A", 50), Draft("B", 50)).Value;
        var (a, b) = (account.SubPortfolios[0].Id, account.SubPortfolios[1].Id);

        account.RetireSubPortfolio(a, new Dictionary<SubPortfolioId, TargetShare> { [b] = TargetShare.TryFromPercent(100).Value }, Now).IsSuccess.ShouldBeTrue();

        account.FindActive(a).ShouldBeNull();
        account.ActiveSubPortfolios.ShouldHaveSingleItem().TargetShare.Value.Value.ShouldBe(1m);
        account.RetireSubPortfolio(b, new Dictionary<SubPortfolioId, TargetShare>(), Now).Error.Code.ShouldBe("account.last_sub_portfolio");
    }
}
