namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record MoneyOutcomes(int Periods, MoneyOutcome? Worst, MoneyOutcome? Typical, MoneyOutcome? Best)
{
    public static MoneyOutcomes Of(IReadOnlyList<ReplayPoint> points, MoneyPlan plan)
    {
        var outcomes = Enumerable.Range(0, Math.Max(0, points.Count - plan.Months))
            .Select(first => OutcomeFrom(points, first, plan))
            .OrderBy(outcome => outcome.Final)
            .ToList();

        return outcomes.Count == 0
            ? new MoneyOutcomes(0, null, null, null)
            : new MoneyOutcomes(outcomes.Count, outcomes[0], outcomes[outcomes.Count / 2], outcomes[^1]);
    }

    private static MoneyOutcome OutcomeFrom(IReadOnlyList<ReplayPoint> points, int first, MoneyPlan plan)
    {
        var value = plan.Start;

        for (var month = first + 1; month <= first + plan.Months; month++)
            value = (value + plan.MonthlyContribution) * (decimal)(points[month].Growth / points[month - 1].Growth);

        return new MoneyOutcome(decimal.Round(value, 2), points[first].Date, points[first + plan.Months].Date);
    }
}
