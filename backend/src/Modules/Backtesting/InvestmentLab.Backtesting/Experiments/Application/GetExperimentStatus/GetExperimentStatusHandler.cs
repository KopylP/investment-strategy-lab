using InvestmentLab.SharedKernel;
using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentStatus;

public sealed class GetExperimentStatusHandler(IExperimentRepository experiments) : IHandler
{
    public async Task<ExperimentStatusDto?> Handle(Guid id, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(id, cancellationToken);
        if (experiment is null)
            return null;

        var summary = experiment.Result?.Baseline.StrategySummary;

        return new ExperimentStatusDto(
            Status: experiment.Status.ToString(),
            FailureReason: experiment.FailureReason,
            Summary: SummaryOf(summary));
    }

    private static BacktestSummaryDto? SummaryOf(ReplaySummary? summary)
    {
        if (summary is null)
            return null;

        return new BacktestSummaryDto(
            TotalReturn: summary.TotalReturn,
            AnnualReturn: summary.AnnualReturn,
            AnnualReturnAfterTax: summary.AnnualReturnAfterTax,
            Volatility: summary.Volatility,
            Sharpe: summary.Sharpe,
            MaxDrawdown: summary.MaxDrawdown);
    }
}
