using System.Globalization;
using System.Text.Json;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public static class IndicatorNaming
{
    private static readonly string[] NotNamed = ["indicator", "of"];

    public static string NameOf(GraphNode indicator, DraftSetup setup)
    {
        var kind = indicator.Parameters.Text("indicator") ?? "indicator";
        var numbers = IntegerParameters(indicator.Parameters).Select(value => value.ToString(CultureInfo.InvariantCulture));

        return kind + string.Concat(numbers) + SeriesSuffix(SeriesChoice.Of(indicator.Parameters), setup);
    }

    private static IEnumerable<long> IntegerParameters(NodeParameters parameters) =>
        parameters.Values
            .Where(entry => !NotNamed.Contains(entry.Key) && entry.Value.ValueKind == JsonValueKind.Number)
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => (long)Math.Round(entry.Value.GetDouble()));

    private static string SeriesSuffix(SeriesChoice? series, DraftSetup setup) => series?.Series switch
    {
        SeriesChoice.Fund => "Fund",
        SeriesChoice.Benchmark => "Benchmark",
        SeriesChoice.VolatilityIndex => "Vix",
        SeriesChoice.Extra => $"Extra{(series.InstrumentId is { } id ? setup.ExtraNumberOf(id) : 0)}",
        _ => "",
    };
}
