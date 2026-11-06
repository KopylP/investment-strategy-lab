namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record BacktestComparison(bool Comparable, string? Reason, BacktestDifference? Difference)
{
    public static BacktestComparison Of(ComparisonSubject first, ComparisonSubject second)
    {
        if (!HaveSameConditions(first, second))
            return new BacktestComparison(false, Message.Of("research.comparison_mismatch"), null);

        var difference = DifferenceOf(first.Result.StrategySummary, second.Result.StrategySummary);

        return new BacktestComparison(true, null, difference);
    }

    private static bool HaveSameConditions(ComparisonSubject first, ComparisonSubject second)
    {
        if (first.Period != second.Period || first.Costs != second.Costs)
            return false;

        if (first.Result.InitialCapital != second.Result.InitialCapital)
            return false;

        if (first.Result.Start != second.Result.Start || first.Result.End != second.Result.End)
            return false;

        return string.Equals(first.Result.Currency, second.Result.Currency, StringComparison.OrdinalIgnoreCase);
    }

    private static BacktestDifference DifferenceOf(ReplaySummary first, ReplaySummary second) => new(
        AnnualReturn: second.AnnualReturn - first.AnnualReturn,
        AnnualReturnAfterTax: AfterTaxDifference(first.AnnualReturnAfterTax, second.AnnualReturnAfterTax),
        Volatility: second.Volatility - first.Volatility,
        Sharpe: second.Sharpe - first.Sharpe,
        MaxDrawdown: second.MaxDrawdown - first.MaxDrawdown);

    private static double? AfterTaxDifference(double? first, double? second) =>
        first is { } firstValue && second is { } secondValue ? secondValue - firstValue : null;
}
