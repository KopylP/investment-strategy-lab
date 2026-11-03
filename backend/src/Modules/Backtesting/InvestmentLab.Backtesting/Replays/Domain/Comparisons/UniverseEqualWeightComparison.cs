namespace InvestmentLab.Backtesting.Replays.Domain.Comparisons;

public sealed class UniverseEqualWeightComparison : IReplayComparison
{
    private static readonly Guid EqualWeightBasket = new("5e1f0c3a-7d2b-4b6e-9a51-0e4c8f2d9b17");

    public string Code => "universe-equal-weight";

    public ComparedLine LineOf(ReplayedHistory history)
    {
        var growth = new List<GrowthDay>();
        var value = 1d;

        for (var dayIndex = history.FirstDay; dayIndex < history.Prices.Days.Count; dayIndex++)
        {
            if (dayIndex > history.FirstDay)
                value *= 1d + AverageReturnOn(history, dayIndex);

            growth.Add(new GrowthDay(history.Prices.Days[dayIndex], value));
        }

        var label = Message.Of("backtest.series.universe-equal-weight");

        return new ComparedLine(Code, label, growth, HeldAs: EqualWeightBasket);
    }

    private static double AverageReturnOn(ReplayedHistory history, int dayIndex)
    {
        var returns = history.Setup.Universe
            .Select(instrumentId => history.Prices.ReturnOn(instrumentId, dayIndex))
            .OfType<double>()
            .ToList();

        return returns.Count > 0 ? returns.Average() : 0d;
    }
}
