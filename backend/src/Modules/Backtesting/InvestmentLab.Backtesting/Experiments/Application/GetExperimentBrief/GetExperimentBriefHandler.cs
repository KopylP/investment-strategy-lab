using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentBrief;

public sealed record GetExperimentBriefQuery(Guid ExperimentId, bool IncludePoints);

public sealed record ExperimentBriefDto(
    Guid Id,
    Guid StrategyId,
    int StrategyVersion,
    string Status,
    string? FailureReason,
    ExperimentConditions Conditions,
    ReplayResultBrief? Result,
    IReadOnlyList<ResearchStudyBrief>? Studies);

public sealed record ResearchStudyBrief(
    string Kind,
    ResearchFold Window,
    decimal? ParameterValue,
    double? Sharpe,
    double? AnnualReturn,
    double? MaxDrawdown,
    double? TrainingSharpe,
    string? FailureReason);

public sealed class GetExperimentBriefHandler(IExperimentRepository experiments)
{
    public async Task<Result<ExperimentBriefDto>> Handle(GetExperimentBriefQuery query, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(query.ExperimentId, cancellationToken);
        if (experiment is null)
            return ExperimentErrors.NotFound(query.ExperimentId);

        var report = experiment.Result;
        var result = ReplayResultBrief.Of(report?.Baseline, query.IncludePoints);
        var studies = report?.Studies.Select(BriefOf).ToList();

        return new ExperimentBriefDto(
            Id: experiment.Id,
            StrategyId: experiment.StrategyId,
            StrategyVersion: experiment.StrategyVersion,
            Status: experiment.Status.ToString(),
            FailureReason: experiment.FailureReason,
            Conditions: experiment.Conditions,
            Result: result,
            Studies: studies);
    }

    private static ResearchStudyBrief BriefOf(ResearchStudy study) => new(
        Kind: study.Kind,
        Window: study.Window,
        ParameterValue: study.ParameterValue,
        Sharpe: study.Summary?.Sharpe,
        AnnualReturn: study.Summary?.AnnualReturn,
        MaxDrawdown: study.Summary?.MaxDrawdown,
        TrainingSharpe: study.TrainingSharpe,
        FailureReason: study.FailureReason);
}
