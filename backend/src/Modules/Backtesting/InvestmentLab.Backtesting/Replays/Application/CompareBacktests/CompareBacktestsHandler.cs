using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.CompareBacktests;

public sealed record CompareBacktestsQuery(BacktestRun First, BacktestRun Second);

public sealed record BacktestComparisonDto(bool Comparable, string? Reason, BacktestDifference? Difference);

public sealed class CompareBacktestsHandler(IBacktestRepository backtests, IExperimentRepository experiments)
{
    public async Task<Result<BacktestComparisonDto>> Handle(CompareBacktestsQuery query, CancellationToken cancellationToken)
    {
        var first = await Subject(query.First, cancellationToken);
        if (first.IsFailure)
            return first.Error;

        var second = await Subject(query.Second, cancellationToken);
        if (second.IsFailure)
            return second.Error;

        var comparison = BacktestComparison.Of(first.Value, second.Value);

        return new BacktestComparisonDto(comparison.Comparable, comparison.Reason, comparison.Difference);
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
