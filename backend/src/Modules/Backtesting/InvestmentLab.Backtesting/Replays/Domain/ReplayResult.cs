using System.Text.Json.Serialization;

namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayResult(DateOnly Start, DateOnly End, string Currency, int Trades, int RiskExits, int SignalReports, IReadOnlyList<ReplaySeries> Series)
{
    [JsonIgnore]
    public ReplaySummary StrategySummary =>
        Series.FirstOrDefault(series => series.Code == ReplaySeries.StrategyCode)?.Summary
        ?? throw new InvalidOperationException("A replay result has no strategy series.");

    public ReplaySeries SeriesOf(string code) => Series.First(series => series.Code == code);
}
