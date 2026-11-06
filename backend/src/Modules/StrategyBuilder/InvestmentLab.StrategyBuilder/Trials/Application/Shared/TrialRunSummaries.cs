using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public sealed class TrialRunSummaries(ITrialBacktests backtests, ITrialExperiments experiments)
{
    public async Task<TrialRunDto> DescribeAsync(TrialRun run, CancellationToken cancellationToken)
    {
        var summary = run.ExperimentId is { } experimentId
            ? await experiments.SummaryOfAsync(experimentId, cancellationToken)
            : await backtests.SummaryOfAsync(run.StrategyId, run.StrategyVersion, cancellationToken);

        return TrialMapping.ToDto(run, summary);
    }
}
