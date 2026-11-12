using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.ListTrialRows;

public sealed record TrialRowsQuery(Guid DraftId, string? LabelContains, string? Status, int Limit);

public sealed record TrialRowDto(
    int Number,
    string? Label,
    string Status,
    string? Period,
    double? AnnualReturn,
    double? Volatility,
    double? Sharpe,
    double? MaxDrawdown,
    string? FailureReason,
    int? QueuePosition,
    Guid StrategyId,
    int Version,
    Guid? ExperimentId);

public sealed class ListTrialRowsHandler(IDraftRepository drafts, TrialRunSummaries summaries, ITrialExperiments experiments) : IHandler
{
    public async Task<Result<IReadOnlyList<TrialRowDto>>> Handle(TrialRowsQuery query, CancellationToken cancellationToken)
    {
        var draft = await drafts.FindAsync(query.DraftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(query.DraftId);

        var labelled = NewestWithLabel(draft.Trials, query.LabelContains);
        var described = await DescribeAsync(labelled, cancellationToken);
        var shown = WithStatus(described, query.Status).Take(query.Limit).ToList();
        var positions = await QueuePositionsAsync(shown, cancellationToken);

        return shown.Select(run => RowOf(run, positions)).ToList();
    }

    private static IEnumerable<TrialRun> NewestWithLabel(IReadOnlyList<TrialRun> trials, string? labelContains)
    {
        var newestFirst = trials.OrderByDescending(run => run.Number);
        if (string.IsNullOrWhiteSpace(labelContains))
            return newestFirst;

        return newestFirst.Where(run => run.Label?.Contains(labelContains, StringComparison.OrdinalIgnoreCase) == true);
    }

    private async Task<IReadOnlyList<TrialRunDto>> DescribeAsync(IEnumerable<TrialRun> trials, CancellationToken cancellationToken)
    {
        var described = new List<TrialRunDto>();
        foreach (var trial in trials)
            described.Add(await summaries.DescribeAsync(trial, cancellationToken));

        return described;
    }

    private static IEnumerable<TrialRunDto> WithStatus(IEnumerable<TrialRunDto> runs, string? status) =>
        string.IsNullOrWhiteSpace(status)
            ? runs
            : runs.Where(run => string.Equals(run.Status, status, StringComparison.OrdinalIgnoreCase));

    private async Task<IReadOnlyDictionary<Guid, int>> QueuePositionsAsync(IReadOnlyList<TrialRunDto> runs, CancellationToken cancellationToken)
    {
        var unfinished = runs.Where(IsUnfinishedExperiment).Select(run => run.ExperimentId!.Value).ToList();
        if (unfinished.Count == 0)
            return new Dictionary<Guid, int>();

        return await experiments.QueuePositionsAsync(unfinished, cancellationToken);
    }

    private static bool IsUnfinishedExperiment(TrialRunDto run) =>
        run.ExperimentId is not null && run.Status is nameof(TrialStatus.Waiting) or nameof(TrialStatus.Running);

    private static TrialRowDto RowOf(TrialRunDto run, IReadOnlyDictionary<Guid, int> positions) => new(
        Number: run.Number,
        Label: run.Label,
        Status: run.Status,
        Period: PeriodOf(run.Experiment),
        AnnualReturn: run.AnnualReturn,
        Volatility: run.Volatility,
        Sharpe: run.Sharpe,
        MaxDrawdown: run.MaxDrawdown,
        FailureReason: run.FailureReason,
        QueuePosition: QueuePositionOf(run, positions),
        StrategyId: run.StrategyId,
        Version: run.Version,
        ExperimentId: run.ExperimentId);

    private static string? PeriodOf(TrialExperimentDto? experiment) =>
        experiment is null ? null : $"{experiment.Period.Start:yyyy-MM-dd}..{experiment.Period.End:yyyy-MM-dd}";

    private static int? QueuePositionOf(TrialRunDto run, IReadOnlyDictionary<Guid, int> positions) =>
        run.ExperimentId is { } id && positions.TryGetValue(id, out var position) ? position : null;
}
