namespace InvestmentLab.Backtesting.Experiments.Domain;

public static class TrainingSelection
{
    public static TrainingResult? HighestSharpe(IReadOnlyList<TrainingResult> results)
    {
        var measured = results.Where(HasFiniteSharpe);
        var ranked = measured.OrderByDescending(result => result.Summary.Sharpe);

        return ranked.FirstOrDefault();
    }

    private static bool HasFiniteSharpe(TrainingResult result) => double.IsFinite(result.Summary.Sharpe);
}
