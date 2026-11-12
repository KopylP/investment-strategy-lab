using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetQueuePositions;

public sealed class GetQueuePositionsHandler(IExperimentRepository experiments) : IHandler
{
    public async Task<IReadOnlyDictionary<Guid, int>> Handle(IReadOnlyList<Guid> experimentIds, CancellationToken cancellationToken)
    {
        var unfinished = await experiments.UnfinishedInOrderAsync(cancellationToken);

        var positions = ExperimentQueue.PositionsOf(unfinished);

        return experimentIds.Where(positions.ContainsKey).ToDictionary(id => id, id => positions[id]);
    }
}
