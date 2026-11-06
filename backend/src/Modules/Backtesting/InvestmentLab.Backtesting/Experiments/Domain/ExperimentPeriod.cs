namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ExperimentPeriod(DateOnly Start, DateOnly End)
{
    private const int MinimumDays = 60;
    private const int MaximumDays = 25 * 366;

    public Error? Problem(DateOnly today)
    {
        if (Start >= End || End > today)
            return ExperimentErrors.InvalidPeriod();

        var days = End.DayNumber - Start.DayNumber;

        return days is < MinimumDays or > MaximumDays
            ? ExperimentErrors.InvalidPeriod()
            : null;
    }

    public int Days => End.DayNumber - Start.DayNumber;
}
