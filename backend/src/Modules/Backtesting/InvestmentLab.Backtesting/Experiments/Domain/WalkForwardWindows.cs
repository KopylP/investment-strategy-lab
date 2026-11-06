namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record WalkForwardWindows(int TrainingMonths, int TestMonths)
{
    private const int MinimumTrainingMonths = 3;
    private const int MaximumTrainingMonths = 120;
    private const int MinimumTestMonths = 1;
    private const int MaximumTestMonths = 36;
    private const int MinimumTestDays = 28;
    private const int MaximumFolds = 6;

    public Error? Problem(ExperimentPeriod period)
    {
        if (TrainingMonths is < MinimumTrainingMonths or > MaximumTrainingMonths)
            return ExperimentErrors.InvalidWindows();

        if (TestMonths is < MinimumTestMonths or > MaximumTestMonths)
            return ExperimentErrors.InvalidWindows();

        var folds = FoldsFor(period);

        if (folds.Count == 0)
            return ExperimentErrors.InvalidWindows();

        return folds.Count > MaximumFolds ? ExperimentErrors.TooManyFolds() : null;
    }

    public IReadOnlyList<ResearchFold> FoldsFor(ExperimentPeriod period)
    {
        var folds = new List<ResearchFold>();
        if (TrainingMonths < 1 || TestMonths < MinimumTestMonths)
            return folds;

        for (var start = period.Start; start < period.End; start = start.AddMonths(TestMonths))
        {
            var fold = FoldFrom(start, period.End);
            if (fold.Test.Days + 1 < MinimumTestDays)
                break;

            folds.Add(fold);
            if (folds.Count > MaximumFolds)
                break;
        }

        return folds;
    }

    private ResearchFold FoldFrom(DateOnly start, DateOnly end)
    {
        var testStart = start.AddMonths(TrainingMonths);
        var proposedEnd = testStart.AddMonths(TestMonths).AddDays(-1);
        var testEnd = proposedEnd < end ? proposedEnd : end;
        var training = new ExperimentPeriod(start, testStart.AddDays(-1));
        var test = new ExperimentPeriod(testStart, testEnd);

        return new ResearchFold(training, test);
    }
}
