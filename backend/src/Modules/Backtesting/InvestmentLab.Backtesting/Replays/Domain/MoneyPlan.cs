namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record MoneyPlan
{
    public decimal Start { get; }
    public decimal MonthlyContribution { get; }
    public int Years { get; }

    private MoneyPlan(decimal start, decimal monthlyContribution, int years) =>
        (Start, MonthlyContribution, Years) = (start, monthlyContribution, years);

    public int Months => Years * 12;

    public decimal Contributed => Start + MonthlyContribution * Months;

    public static Result<MoneyPlan> TryOf(decimal start, decimal monthlyContribution, int years) =>
        start < 0m || monthlyContribution < 0m || years < 1
            ? BacktestErrors.InvalidMoneyPlan()
            : new MoneyPlan(start, monthlyContribution, years);
}
