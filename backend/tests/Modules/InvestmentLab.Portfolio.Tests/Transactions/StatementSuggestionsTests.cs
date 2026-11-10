using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Tests.Transactions;

public sealed class StatementSuggestionsTests
{
    private static readonly Guid Etf = Guid.NewGuid(), Strategy = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> NoUniverses = new Dictionary<Guid, IReadOnlyList<Guid>>();

    private static SubPortfolioDraft Draft(string name, decimal percent) =>
        new(name, TargetShare.TryFromPercent(percent).Value, null, null, Guardrails.None);

    private static InvestmentAccount Account(params SubPortfolioDraft[] drafts) =>
        InvestmentAccount.Open(UserId.New(), "Main", "Broker", Currency.EUR, "TEST-POLICY", ContributionPolicy.Default, drafts, Now).Value;

    private static SubPortfolioEvidence Evidence(
        InvestmentAccount account, IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? universes = null, params (SubPortfolio Sub, decimal Units)[] held) =>
        new(account, Holdings(account, held), universes ?? NoUniverses);

    private static AccountHoldings Holdings(InvestmentAccount account, (SubPortfolio Sub, decimal Units)[] held)
    {
        var subs = held.Select(each => new SubPortfolioHoldings(
            each.Sub.Id,
            Money.Zero(account.BaseCurrency),
            [],
            [new HoldingLine(Etf, [new VirtualLot(TransactionId.New(), DateOnly.FromDateTime(Now.Date), Quantity.Of(each.Units), 10m)])]));

        return new AccountHoldings(account.Id, account.BaseCurrency, null, [.. subs]);
    }

    private static StatementRow Buy(string ticker, decimal amount) =>
        new(1, Now, StatementRowKind.Buy, "BUY", ticker, 1m, amount, amount, "EUR", ticker);

    [Fact]
    public void The_only_sub_portfolio_takes_any_ticker_even_an_unknown_one()
    {
        var account = Account(Draft("A", 100));

        StatementSuggestions.SubPortfolioFor(null, Evidence(account)).ShouldBe(account.SubPortfolios[0].Id.Value);
    }

    [Fact]
    public void An_unknown_ticker_goes_to_the_sub_portfolio_with_the_largest_target_share()
    {
        var account = Account(Draft("A", 30), Draft("B", 70));

        StatementSuggestions.SubPortfolioFor(null, Evidence(account)).ShouldBe(account.SubPortfolios[1].Id.Value);
        StatementSuggestions.SubPortfolioFor(Etf, Evidence(account)).ShouldBe(account.SubPortfolios[1].Id.Value);
    }

    [Fact]
    public void Between_equal_target_shares_the_first_sub_portfolio_takes_an_unknown_ticker()
    {
        var account = Account(Draft("A", 50), Draft("B", 50));

        StatementSuggestions.SubPortfolioFor(null, Evidence(account)).ShouldBe(account.SubPortfolios[0].Id.Value);
    }

    [Fact]
    public void An_instrument_goes_to_the_sub_portfolio_whose_strategy_has_it_in_its_universe()
    {
        var account = Account(Draft("A", 70), Draft("B", 30));
        var second = account.SubPortfolios[1];
        account.AssignStrategy(second.Id, new StrategyRef(Strategy, null));
        var universes = new Dictionary<Guid, IReadOnlyList<Guid>> { [Strategy] = [Etf] };

        StatementSuggestions.SubPortfolioFor(Etf, Evidence(account, universes)).ShouldBe(second.Id.Value);
    }

    [Fact]
    public void An_instrument_already_held_goes_to_the_sub_portfolio_holding_most_of_it_before_any_universe()
    {
        var account = Account(Draft("A", 50), Draft("B", 30), Draft("C", 20));
        var (first, second, third) = (account.SubPortfolios[0], account.SubPortfolios[1], account.SubPortfolios[2]);
        account.AssignStrategy(first.Id, new StrategyRef(Strategy, null));
        var universes = new Dictionary<Guid, IReadOnlyList<Guid>> { [Strategy] = [Etf] };

        var evidence = Evidence(account, universes, (second, 2m), (third, 5m));

        StatementSuggestions.SubPortfolioFor(Etf, evidence).ShouldBe(third.Id.Value);
    }

    [Fact]
    public void A_retired_sub_portfolio_never_takes_a_ticker_even_if_it_holds_it()
    {
        var account = Account(Draft("A", 60), Draft("B", 40));
        var (first, second) = (account.SubPortfolios[0], account.SubPortfolios[1]);
        var shareOfRemaining = new Dictionary<SubPortfolioId, TargetShare> { [first.Id] = TargetShare.TryFromPercent(100).Value };
        account.RetireSubPortfolio(second.Id, shareOfRemaining, Now);

        StatementSuggestions.SubPortfolioFor(Etf, Evidence(account, null, (second, 5m))).ShouldBe(first.Id.Value);
    }

    [Fact]
    public void Cash_goes_to_the_sub_portfolio_most_was_spent_on()
    {
        var account = Account(Draft("A", 50), Draft("B", 50));
        var (first, second) = (account.SubPortfolios[0].Id.Value, account.SubPortfolios[1].Id.Value);
        TickerMapping[] mappings = [new("AAA", "EUR", Etf, null, first), new("BBB", "EUR", Etf, null, second)];
        StatementRow[] rows = [Buy("AAA", 10m), Buy("bbb", 20m), Buy("AAA", 5m)];

        StatementSuggestions.CashSubPortfolio(account, rows, mappings).ShouldBe(second);
    }

    [Fact]
    public void Without_mapped_buys_cash_goes_to_the_sub_portfolio_with_the_largest_target_share()
    {
        var account = Account(Draft("A", 40), Draft("B", 60));

        StatementSuggestions.CashSubPortfolio(account, [], []).ShouldBe(account.SubPortfolios[1].Id.Value);
    }
}
