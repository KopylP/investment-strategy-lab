using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed record SeriesChoice(string Series, Guid? InstrumentId)
{
    public const string Fund = "fund";
    public const string Benchmark = "benchmark";
    public const string VolatilityIndex = "volatilityIndex";
    public const string Extra = "extra";

    public static SeriesChoice? Of(NodeParameters parameters)
    {
        if (parameters.Element("of") is not { ValueKind: JsonValueKind.Object } of)
            return null;
        if (!of.TryGetProperty("series", out var series) || series.ValueKind != JsonValueKind.String)
            return null;

        var instrument = of.TryGetProperty("instrumentId", out var id) && id.ValueKind == JsonValueKind.String && Guid.TryParse(id.GetString(), out var parsed)
            ? parsed
            : (Guid?)null;

        return new SeriesChoice(series.GetString()!, instrument);
    }
}
