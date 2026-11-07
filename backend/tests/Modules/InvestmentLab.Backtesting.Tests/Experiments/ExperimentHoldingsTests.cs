using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExperimentHoldingsTests
{
    private static readonly Guid Stocks = Guid.NewGuid();
    private static readonly Guid Bonds = Guid.NewGuid();
    private static readonly Guid CashFund = Guid.NewGuid();
    private static readonly DateOnly Start = new(2020, 1, 1);

    private static readonly IReadOnlyDictionary<Guid, string> Labels = new Dictionary<Guid, string>
    {
        [Stocks] = "Stocks",
        [Bonds] = "Bonds",
        [CashFund] = "Cash fund",
    };

    [Fact]
    public void An_unfavourable_market_counts_in_the_share_of_unfavourable_rebalances()
    {
        var holdings = HoldingsOf(
            Rebalance(Start, true, (Stocks, 0.6m), (Bonds, 0.4m)),
            Rebalance(Start.AddMonths(1), false, (Stocks, 0.3m), (CashFund, 0.7m)),
            Rebalance(Start.AddMonths(2), true, (Stocks, 0.5m), (Bonds, 0.5m)));

        holdings.Diagnostics.UnfavorableShare!.Value.ShouldBe(1d / 3d, 1e-9);
    }

    [Fact]
    public void The_cash_fund_is_counted_as_cash_and_not_as_a_held_fund()
    {
        var holdings = HoldingsOf(
            Rebalance(Start, true, (Stocks, 0.6m), (Bonds, 0.4m)),
            Rebalance(Start.AddMonths(1), false, (Stocks, 0.3m), (CashFund, 0.7m)));

        var parked = holdings.Rows[1];
        parked.CashShare.ShouldBe(0.7d, 1e-9);
        parked.Positions.ShouldHaveSingleItem().Fund.ShouldBe("Stocks");
        holdings.Diagnostics.MeanFunds.ShouldBe(1.5d, 1e-9);
        holdings.Diagnostics.MeanInvestedShare.ShouldBe(0.65d, 1e-9);
    }

    [Fact]
    public void Entries_stored_before_the_market_view_was_recorded_count_as_unknown()
    {
        var holdings = HoldingsOf(
            Rebalance(Start, null, (Stocks, 1m)),
            Rebalance(Start.AddMonths(1), false, (Stocks, 1m)));

        holdings.Rows[0].MarketFavorable.ShouldBeNull();
        holdings.Diagnostics.UnfavorableShare!.Value.ShouldBe(1d, 1e-9);
    }

    [Fact]
    public void Trades_per_year_spread_the_trade_count_over_the_run()
    {
        var holdings = HoldingsOf(Rebalance(Start, true, (Stocks, 1m)));

        holdings.Diagnostics.TradesPerYear.ShouldBe(12d, 0.05);
    }

    private static ExperimentHoldings HoldingsOf(params ReplayJournalEntry[] entries)
    {
        var run = new ReplayResult(
            Start: Start,
            End: Start.AddYears(2),
            Currency: "EUR",
            Trades: 24,
            RiskExits: 0,
            SignalReports: 0,
            Series: [],
            Journal: entries);

        return ExperimentHoldings.Of(run, Labels, CashFund);
    }

    private static ReplayJournalEntry Rebalance(DateOnly date, bool? favorable, params (Guid Fund, decimal Weight)[] positions)
    {
        var weights = positions.Select(position => new ReplayPositionWeight(position.Fund, 1m, position.Weight)).ToList();
        var allocation = new ReplayAllocation(Cash: 0m, Targets: [], Positions: weights);

        return new ReplayJournalEntry(date, "rebalance", "reason", Allocation: allocation, MarketFavorable: favorable);
    }
}
