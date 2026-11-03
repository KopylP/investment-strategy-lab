namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayResult(DateOnly Start, DateOnly End, string Currency, int Trades, int RiskExits, int SignalReports, IReadOnlyList<ReplaySeries> Series)
{
    public ReplaySeries SeriesOf(string code) => Series.First(series => series.Code == code);
}
