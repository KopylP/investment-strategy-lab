namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record RobustnessPlan(
    DateOnly? OutOfSampleStart = null,
    WalkForwardWindows? WalkForward = null,
    ParameterNeighborhood? Neighborhood = null)
{
    private const int MinimumTrainingDays = 60;
    private const int MinimumTestDays = 30;

    public Error? Problem(ExperimentPeriod period)
    {
        if (IndependentTestProblem(period) is { } splitProblem)
            return splitProblem;

        if (WalkForward?.Problem(period) is { } walkForwardProblem)
            return walkForwardProblem;

        return Neighborhood?.Problem();
    }

    public ResearchFold? IndependentTest(ExperimentPeriod period)
    {
        if (OutOfSampleStart is not { } split)
            return null;

        var training = new ExperimentPeriod(period.Start, split.AddDays(-1));
        var test = new ExperimentPeriod(split, period.End);

        return new ResearchFold(training, test);
    }

    private Error? IndependentTestProblem(ExperimentPeriod period)
    {
        if (OutOfSampleStart is not { } split)
            return null;

        var trainingDays = split.DayNumber - period.Start.DayNumber;
        var testDays = period.End.DayNumber - split.DayNumber;
        if (trainingDays < MinimumTrainingDays || testDays < MinimumTestDays)
            return ExperimentErrors.InvalidSplit();

        return null;
    }
}
