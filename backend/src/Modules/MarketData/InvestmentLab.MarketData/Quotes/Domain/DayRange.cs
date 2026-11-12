namespace InvestmentLab.MarketData.Quotes.Domain;

public readonly record struct DayRange(decimal Open, decimal High, decimal Low)
{
    public static DayRange? Of(decimal? open, decimal? high, decimal? low, decimal close)
    {
        if (open is not > 0m || high is not > 0m || low is not > 0m)
            return null;

        var highestOfBody = Math.Max(open.Value, close);
        var lowestOfBody = Math.Min(open.Value, close);
        var wicksEncloseBody = high >= highestOfBody && low <= lowestOfBody;

        return wicksEncloseBody ? new DayRange(open.Value, high.Value, low.Value) : null;
    }
}
