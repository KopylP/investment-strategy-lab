using InvestmentLab.Portfolio.Accounts.Domain;
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

    private static StatementRow Buy(string ticker, decimal amount) =>
        new(1, Now, StatementRowKind.Buy, "BUY", ticker, 1m, amount, amount, "EUR", ticker);

    [Fact]
    public void The_only_sub_portfolio_takes_any_ticker_even_an_unknown_one()
    {
        var account = Account(Draft("A", 100));

        StatementSuggestions.SubPortfolioFor(null, account, NoUniverses).ShouldBe(account.SubPortfolios[0].Id.Value);
    }

    [Fact]
    public void With_several_sub_portfolios_an_unknown_ticker_stays_for_the_user_to_choose()
    {
        var account = Account(Draft("A", 50), Draft("B", 50));

        StatementSuggestions.SubPortfolioFor(null, account, NoUniverses).ShouldBeNull();
        StatementSuggestions.SubPortfolioFor(Etf, account, NoUniverses).ShouldBeNull();
    }

    [Fact]
    public void An_instrument_goes_to_the_sub_portfolio_whose_strategy_has_it_in_its_universe()
    {
        var account = Account(Draft("A", 50), Draft("B", 50));
        var second = account.SubPortfolios[1];
        account.AssignStrategy(second.Id, new StrategyRef(Strategy, null));
        var universes = new Dictionary<Guid, IReadOnlyList<Guid>> { [Strategy] = [Etf] };

        StatementSuggestions.SubPortfolioFor(Etf, account, universes).ShouldBe(second.Id.Value);
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
    public void Without_mapped_buys_cash_goes_to_the_first_active_sub_portfolio()
    {
        var account = Account(Draft("A", 50), Draft("B", 50));

        StatementSuggestions.CashSubPortfolio(account, [], []).ShouldBe(account.SubPortfolios[0].Id.Value);
    }
}
