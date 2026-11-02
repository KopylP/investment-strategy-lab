namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayResult(DateOnly Start, DateOnly End, int Trades, int RiskExits, int SignalReports, IReadOnlyList<ReplaySeries> Series)
{
    public ReplaySeries SeriesOf(ReplaySeriesKind kind) => Series.First(series => series.Kind == kind);
}
