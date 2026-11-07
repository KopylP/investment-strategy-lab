namespace InvestmentLab.Backtesting.Experiments.Domain;

public static class ExperimentQueue
{
    public const int Running = 0;

    public static IReadOnlyDictionary<Guid, int> PositionsOf(IReadOnlyList<UnfinishedExperiment> unfinishedInOrder)
    {
        var positions = new Dictionary<Guid, int>();
        var waiting = 0;

        foreach (var experiment in unfinishedInOrder)
        {
            var position = experiment.Status == ExperimentStatus.Running ? Running : ++waiting;
            positions[experiment.Id] = position;
        }

        return positions;
    }
}
