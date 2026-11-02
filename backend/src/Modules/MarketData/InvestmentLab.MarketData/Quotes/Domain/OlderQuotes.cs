namespace InvestmentLab.MarketData.Quotes.Domain;

public static class OlderQuotes
{
    private const int AdjustedCloseDecimals = 8;

    public static IReadOnlyList<ArchivedPrice> Before(IReadOnlyList<ArchivedPrice> archived, FirstStoredQuote? firstStored)
    {
        if (firstStored is null)
            return archived;

        var scale = ScaleAtSeam(archived, firstStored);
        var older = archived.Where(price => price.Date < firstStored.Date);

        return [.. older.Select(price => price with { AdjustedClose = decimal.Round(price.AdjustedClose * scale, AdjustedCloseDecimals) })];
    }

    private static decimal ScaleAtSeam(IReadOnlyList<ArchivedPrice> archived, FirstStoredQuote firstStored)
    {
        var seam = archived.LastOrDefault(price => price.Date <= firstStored.Date);

        return seam.AdjustedClose > 0m ? firstStored.AdjustedClose / seam.AdjustedClose : 1m;
    }
}
