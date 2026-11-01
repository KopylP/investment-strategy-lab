namespace InvestmentLab.Backtesting.Replays.Domain;

[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<ReplaySeriesKind>))]
public enum ReplaySeriesKind
{
    Strategy,
    Benchmark,
    EqualWeight,
}
