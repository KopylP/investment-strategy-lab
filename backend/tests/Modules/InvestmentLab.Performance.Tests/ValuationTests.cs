using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Tests;

public sealed class ValuationTests
{
    private static readonly Guid Sleeve = Guid.NewGuid(), Other = Guid.NewGuid(), Etf = Guid.NewGuid();
    private static readonly DateOnly D1 = new(2026, 1, 5), D2 = new(2026, 1, 6), D3 = new(2026, 1, 7);

    [Fact]
    public void Twr_ignores_new_money_and_counts_it_at_the_start_of_the_day()
    {
        var prices = new Dictionary<DateOnly, decimal> { [D1] = 100m, [D2] = 110m, [D3] = 121m };
        var snapshots = ValuationCalculator.Build([
            new Movement(D1, 1, Sleeve, null, 0m, 1000m, 1000m),
            new Movement(D1, 2, Sleeve, Etf, 10m, -1000m, 0m, TradePrice: 100m),
            new Movement(D3, 3, Sleeve, null, 0m, 1100m, 1100m),
        ], [D1, D2, D3], (_, day) => prices[day]);

        var account = snapshots.Where(snapshot => snapshot.SubPortfolioId is null).ToList();
        account.Select(snapshot => snapshot.Value).ShouldBe([1000m, 1100m, 2310m]);
        account[1].TwrIndex.ShouldBe(1.1m);
        account[2].TwrIndex.ShouldBe(1.155m);
        account[2].NetContributions.ShouldBe(2100m);
    }

    [Fact]
    public void Cost_basis_is_what_was_paid_for_the_units_still_held_first_in_first_out_with_fees()
    {
        var snapshots = ValuationCalculator.Build([
            new Movement(D1, 1, Sleeve, null, 0m, 5000m, 5000m),
            new Movement(D1, 2, Sleeve, Etf, 10m, -1010m, 0m, TradePrice: 100m, UnitCost: 101m),
            new Movement(D2, 3, Sleeve, Etf, 10m, -1200m, 0m, TradePrice: 120m),
            new Movement(D3, 4, Sleeve, Etf, -15m, 2100m, 0m, TradePrice: 140m),
        ], [D1, D2, D3], (_, _) => 140m);

        var account = snapshots.Where(snapshot => snapshot.SubPortfolioId is null).ToList();
        account.Select(snapshot => snapshot.CostBasis).ShouldBe([1010m, 2210m, 600m]);
        (account[2].Value - account[2].Cash).ShouldBe(700m);
    }

    [Fact]
    public void A_transfer_moves_the_cost_between_sleeves_and_leaves_the_account_cost_alone()
    {
        var snapshots = ValuationCalculator.Build([
            new Movement(D1, 1, Sleeve, Etf, 10m, -1000m, 0m, TradePrice: 100m),
            new Movement(D1, 2, Sleeve, Etf, 10m, -2000m, 0m, TradePrice: 200m),
            new Movement(D2, 3, Sleeve, Etf, -15m, 0m, 0m, TransferQuantity: -15m),
            new Movement(D2, 4, Other, Etf, 15m, 0m, 0m, TransferQuantity: 15m),
        ], [D1, D2], (_, _) => 200m);

        var last = snapshots.Where(snapshot => snapshot.Date == D2).ToList();
        last.Single(snapshot => snapshot.SubPortfolioId == Sleeve).CostBasis.ShouldBe(1000m);
        last.Single(snapshot => snapshot.SubPortfolioId == Other).CostBasis.ShouldBe(2000m);
        last.Single(snapshot => snapshot.SubPortfolioId is null).CostBasis.ShouldBe(3000m);
    }

    [Fact]
    public void A_dollar_balance_is_valued_at_each_day_rate_so_the_rate_moves_value_but_is_not_a_flow()
    {
        var rates = new Dictionary<DateOnly, decimal> { [D1] = 0.90m, [D2] = 0.90m, [D3] = 0.85m };
        var snapshots = ValuationCalculator.Build([
            new Movement(D1, 1, Sleeve, null, 0m, 100m, 90m, CashCurrency: "USD"),
        ], [D1, D2, D3], (_, _) => null, (currency, day) => currency == "USD" ? rates[day] : null);

        var account = snapshots.Where(snapshot => snapshot.SubPortfolioId is null).ToList();
        account.Select(snapshot => snapshot.Value).ShouldBe([90m, 90m, 85m]);
        account.Select(snapshot => snapshot.Cash).ShouldBe([90m, 90m, 85m]);
        account[2].NetContributions.ShouldBe(90m);
        account[2].TwrIndex.ShouldBe(0.94444444m);
    }

    [Fact]
    public void An_internal_transfer_is_a_flow_for_each_sleeve_but_not_for_the_account()
    {
        var snapshots = ValuationCalculator.Build([
            new Movement(D1, 1, Sleeve, null, 0m, 1000m, 1000m),
            new Movement(D1, 2, Sleeve, Etf, 10m, -1000m, 0m, TradePrice: 100m),
            new Movement(D2, 3, Sleeve, Etf, -5m, 0m, 0m, TransferQuantity: -5m),
            new Movement(D2, 4, Other, Etf, 5m, 0m, 0m, TransferQuantity: 5m),
        ], [D1, D2], (_, _) => 100m);

        var day2 = snapshots.Where(snapshot => snapshot.Date == D2).ToList();
        day2.Single(s => s.SubPortfolioId == Sleeve).Flow.ShouldBe(-500m);
        day2.Single(s => s.SubPortfolioId == Other).Flow.ShouldBe(500m);
        day2.Single(s => s.SubPortfolioId is null).Flow.ShouldBe(0m);
        day2.Single(s => s.SubPortfolioId == Other).TwrIndex.ShouldBe(1m);
    }

    [Fact]
    public void Xirr_of_ten_percent_a_year()
    {
        XirrCalculator.Of([(new DateOnly(2025, 1, 1), -1000m), (new DateOnly(2026, 1, 1), 1100m)])!.Value.ShouldBe(0.1m, 0.0001m);
        XirrCalculator.Of([(new DateOnly(2025, 1, 1), -1000m)]).ShouldBeNull();
    }

    [Fact]
    public void Drawdown_is_measured_from_the_highest_peak()
    {
        var drawdown = DrawdownCalculator.Of([(D1, 1.0m), (D2, 1.2m), (D3, 0.9m), (new DateOnly(2026, 1, 8), 1.08m)]);

        drawdown.Max.ShouldBe(-0.25m);
        drawdown.Current.ShouldBe(-0.1m);
        (drawdown.PeakDate, drawdown.TroughDate).ShouldBe((D2, D3));
    }
}
