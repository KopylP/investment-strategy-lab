using InvestmentLab.Performance.Valuation.Application.GetAllAccountsDashboard;
using InvestmentLab.Performance.Valuation.Application.Shared;
using InvestmentLab.Performance.Valuation.Domain;
using InvestmentLab.Portfolio.Contracts.Accounts;

namespace InvestmentLab.Performance.Tests;

public sealed class AllAccountsDashboardTests
{
    private static readonly DateOnly D1 = new(2026, 1, 5), D2 = new(2026, 1, 6), D3 = new(2026, 1, 7), D4 = new(2026, 1, 8);

    private static Snapshot Day(DateOnly date, decimal value, decimal flow = 0m, decimal index = 1m) =>
        new(date, null, value, Cash: value, flow, NetContributions: value, index, CostBasis: value);

    private static AccountWithHistory Account(string currency, params Snapshot[] series)
    {
        var setup = new AccountSetupDto(Guid.NewGuid(), Guid.NewGuid(), currency, "Broker", currency, "none", "none", 0m, []);

        return new AccountWithHistory(setup, new AccountHistory(series, [], null, []));
    }

    private static IReadOnlyList<Snapshot> Combined(FxOn fx, params AccountWithHistory[] accounts)
    {
        IReadOnlyList<DateOnly> days = [.. accounts.SelectMany(each => each.History.Series.Select(snapshot => snapshot.Date)).Distinct().Order()];
        string[] currencies = [.. accounts.Select(each => each.Account.BaseCurrency)];

        return CombinedAccountHistory.Of(accounts, days, CarriedFx.Over(fx, currencies, days)).Series;
    }

    [Fact]
    public void A_day_without_a_rate_uses_the_last_known_rate_and_keeps_the_history_alive()
    {
        var account = Account("USD", Day(D1, 100m, flow: 100m), Day(D2, 100m), Day(D3, 100m), Day(D4, 100m));

        var series = Combined((_, day) => day == D2 ? null : 2m, account);

        series.Select(snapshot => snapshot.Value).ShouldBe([200m, 200m, 200m, 200m]);
        series.Select(snapshot => snapshot.TwrIndex).ShouldAllBe(index => index == 1m);
    }

    [Fact]
    public void A_later_period_opens_after_a_day_without_a_rate()
    {
        var months = new[] { D1.AddMonths(-2), D1.AddMonths(-2).AddDays(1), D3, D4 };
        var account = Account("USD", Day(months[0], 100m, flow: 100m), Day(months[1], 100m), Day(months[2], 110m), Day(months[3], 121m));
        var series = Combined((_, day) => day == months[1] ? null : 2m, account);

        var summary = DashboardSummary.Of(series, null, "1M");

        summary.Dashboard.Value.ShouldBe(242m);
        summary.Dashboard.Twr.ShouldBe(0.21m);
        summary.Dashboard.Series.Count.ShouldBe(2);
    }

    [Fact]
    public void Days_before_the_first_known_rate_use_that_first_rate()
    {
        var account = Account("USD", Day(D1, 100m, flow: 100m), Day(D2, 100m), Day(D3, 100m));

        var series = Combined((_, day) => day >= D3 ? 2m : null, account);

        series.Select(snapshot => snapshot.Value).ShouldBe([200m, 200m, 200m]);
        series.Select(snapshot => snapshot.TwrIndex).ShouldAllBe(index => index == 1m);
    }

    [Fact]
    public void An_account_whose_currency_never_has_a_rate_stays_out_of_the_whole_history()
    {
        var euro = Account("EUR", Day(D1, 100m, flow: 100m), Day(D2, 100m));
        var usd = Account("USD", Day(D1, 500m, flow: 500m), Day(D2, 500m));

        var series = Combined((currency, _) => currency == "EUR" ? 1m : null, euro, usd);

        series.Select(snapshot => snapshot.Value).ShouldBe([100m, 100m]);
        series.Select(snapshot => snapshot.TwrIndex).ShouldAllBe(index => index == 1m);
    }

    [Fact]
    public void A_period_that_starts_after_the_index_fell_to_zero_does_not_fail()
    {
        var first = D1.AddMonths(-3);
        var history = new[] { Day(first, 100m, flow: 100m), Day(first.AddDays(1), 0m, index: 0m), Day(D1, 0m, index: 0m), Day(D2, 50m, flow: 50m, index: 0m) };

        var summary = DashboardSummary.Of(history, null, "1M");

        summary.Dashboard.Twr.ShouldBeNull();
        summary.Dashboard.Series.ShouldNotBeEmpty();
    }
}
