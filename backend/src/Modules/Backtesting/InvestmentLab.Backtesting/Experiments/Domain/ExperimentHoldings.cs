using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ExperimentHoldings(IReadOnlyList<HoldingsRow> Rows, HoldingsDiagnostics Diagnostics)
{
    private const string RebalanceKind = "rebalance";
    private const double DaysPerYear = 365.25;

    public static ExperimentHoldings Of(ReplayResult run, IReadOnlyDictionary<Guid, string> labels, Guid? cashFundId)
    {
        var rebalances = (run.Journal ?? []).Where(IsRebalanceWithAllocation);
        var rows = rebalances.Select(entry => RowOf(entry, labels, cashFundId)).ToList();

        var diagnostics = new HoldingsDiagnostics(
            UnfavorableShare: UnfavorableShareOf(rows),
            MeanInvestedShare: rows.Count > 0 ? rows.Average(row => row.InvestedShare) : 0d,
            MeanFunds: rows.Count > 0 ? rows.Average(row => row.Positions.Count) : 0d,
            TradesPerYear: TradesPerYearOf(run));

        return new ExperimentHoldings(rows, diagnostics);
    }

    public ExperimentHoldings Between(DateOnly? from, DateOnly? to)
    {
        var selected = Rows.Where(row => row.IsWithin(from, to)).ToList();

        return this with { Rows = selected };
    }

    private static bool IsRebalanceWithAllocation(ReplayJournalEntry entry) =>
        entry.Kind == RebalanceKind && entry.Allocation is not null;

    private static HoldingsRow RowOf(ReplayJournalEntry entry, IReadOnlyDictionary<Guid, string> labels, Guid? cashFundId)
    {
        var funds = entry.Allocation!.Positions
            .Where(position => position.InstrumentId != cashFundId && position.Weight > 0m)
            .OrderByDescending(position => position.Weight)
            .Select(position => ShareOf(position, labels))
            .ToList();
        var invested = funds.Sum(fund => fund.Weight);

        return new HoldingsRow(entry.Date, entry.MarketFavorable, funds, CashShare: 1d - invested);
    }

    private static HeldShare ShareOf(ReplayPositionWeight position, IReadOnlyDictionary<Guid, string> labels)
    {
        var label = labels.GetValueOrDefault(position.InstrumentId) ?? position.InstrumentId.ToString();

        return new HeldShare(label, (double)position.Weight);
    }

    private static double? UnfavorableShareOf(IReadOnlyList<HoldingsRow> rows)
    {
        var known = rows.Where(row => row.MarketFavorable is not null).ToList();
        if (known.Count == 0)
            return null;

        var unfavorable = known.Count(row => row.MarketFavorable == false);

        return (double)unfavorable / known.Count;
    }

    private static double TradesPerYearOf(ReplayResult run)
    {
        var years = (run.End.DayNumber - run.Start.DayNumber) / DaysPerYear;

        return years > 0d ? run.Trades / years : run.Trades;
    }
}
