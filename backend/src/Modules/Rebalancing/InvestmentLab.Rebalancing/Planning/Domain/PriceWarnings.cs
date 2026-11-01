using System.Globalization;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public static class PriceWarnings
{
    public static IEnumerable<string> For(
        IEnumerable<Guid> needed,
        IReadOnlyDictionary<Guid, IInstrumentPrice> prices,
        IReadOnlyDictionary<Guid, string> labels)
    {
        foreach (var id in needed.Distinct())
        {
            if (!prices.TryGetValue(id, out var price))
                yield return Message.Of("report.warning.no_quotes", new { label = LabelOf(id, labels) });
            else if (price.IsStale)
                yield return Message.Of("report.warning.stale_quote", new { label = LabelOf(id, labels), date = DateOf(price) });
        }
    }

    private static string LabelOf(Guid id, IReadOnlyDictionary<Guid, string> labels) =>
        labels.TryGetValue(id, out var label) ? label : id.ToString();

    private static string DateOf(IInstrumentPrice price) => price.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
