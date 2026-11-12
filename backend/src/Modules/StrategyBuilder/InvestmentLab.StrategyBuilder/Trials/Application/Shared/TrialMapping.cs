using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.Shared;

public static class TrialMapping
{
    public static TrialRunDto ToDto(TrialRun run, TrialSummary summary) => new(
        Number: run.Number,
        StrategyId: run.StrategyId,
        Version: run.StrategyVersion,
        Label: run.Label,
        RanAt: run.RanAt,
        Status: summary.Status.ToString(),
        FailureReason: summary.FailureReason,
        AnnualReturn: summary.AnnualReturn,
        AnnualReturnAfterTax: summary.AnnualReturnAfterTax,
        Volatility: summary.Volatility,
        Sharpe: summary.Sharpe,
        MaxDrawdown: summary.MaxDrawdown,
        ExperimentId: run.ExperimentId,
        Experiment: run.Experiment is { } experiment ? TrialExperimentDto.From(experiment) : null);
}
