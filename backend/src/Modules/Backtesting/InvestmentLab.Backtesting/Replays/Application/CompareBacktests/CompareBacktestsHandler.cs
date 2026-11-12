using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.CompareBacktests;

public sealed record CompareBacktestsQuery(BacktestRunDto First, BacktestRunDto Second);

public sealed record BacktestComparisonDto(bool Comparable, string? Reason, BacktestDifferenceDto? Difference);

public sealed class CompareBacktestsHandler(IBacktestRepository backtests, IExperimentRepository experiments)
{
    public async Task<Result<BacktestComparisonDto>> Handle(CompareBacktestsQuery query, CancellationToken cancellationToken)
    {
        var first = await Subject(query.First.ToDomain(), cancellationToken);
        if (first.IsFailure)
            return first.Error;

        var second = await Subject(query.Second.ToDomain(), cancellationToken);
        if (second.IsFailure)
            return second.Error;

        var comparison = BacktestComparison.Of(first.Value, second.Value);

        var difference = comparison.Difference is { } found ? BacktestDifferenceDto.From(found) : null;

        return new BacktestComparisonDto(comparison.Comparable, comparison.Reason, difference);
    }

    private async Task<Result<ComparisonSubject>> Subject(BacktestRun run, CancellationToken cancellationToken)
    {
        if (run.ExperimentId is { } id)
            return await Experiment(id, cancellationToken);

        var backtest = await backtests.FindAsync(run.StrategyId, run.Version, cancellationToken);
        if (backtest is null)
            return BacktestErrors.StrategyNotFound(run.StrategyId);
        if (backtest.Status != BacktestStatus.Succeeded || backtest.Result is null)
            return BacktestErrors.NotReady(run.StrategyId);

        return new ComparisonSubject(backtest.Result);
    }

    private async Task<Result<ComparisonSubject>> Experiment(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(id, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(id);
        if (experiment.Result is null)
            return BacktestErrors.NotReady(experiment.StrategyId);

        return new ComparisonSubject(experiment.Result.Baseline, experiment.Conditions);
    }
}
