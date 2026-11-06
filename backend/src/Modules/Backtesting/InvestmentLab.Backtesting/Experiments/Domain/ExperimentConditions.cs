using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ExperimentConditions(
    ExperimentPeriod Period,
    decimal InitialCapital,
    TradingCosts Costs,
    RobustnessPlan Robustness)
{
    private const decimal MinimumCapital = 100m;
    private const decimal MaximumCapital = 1_000_000_000m;

    public Error? Problem(DateOnly today)
    {
        if (Period.Problem(today) is { } periodProblem)
            return periodProblem;

        if (InitialCapital is < MinimumCapital or > MaximumCapital)
            return ExperimentErrors.InvalidCapital();

        if (Costs.Problem(InitialCapital) is { } costsProblem)
            return costsProblem;

        return Robustness.Problem(Period);
    }
}
