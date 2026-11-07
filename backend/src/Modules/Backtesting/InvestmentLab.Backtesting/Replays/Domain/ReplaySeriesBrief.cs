namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySeriesBrief(
    string Code,
    string Label,
    ReplaySummary Summary,
    SeriesHorizons Horizons,
    IReadOnlyList<ReplayPoint>? Points)
{
    public static ReplaySeriesBrief Of(ReplaySeries series, DateOnly start, bool includePoints) => new(
        Code: series.Code,
        Label: series.Label,
        Summary: series.Summary,
        Horizons: SeriesHorizons.Of(start, series.Points),
        Points: includePoints ? series.Points : null);
}
