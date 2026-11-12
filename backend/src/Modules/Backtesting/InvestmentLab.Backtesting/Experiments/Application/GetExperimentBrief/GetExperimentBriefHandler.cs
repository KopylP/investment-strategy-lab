using InvestmentLab.Backtesting.Experiments.Application.Shared;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Application.Shared;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.GetExperimentBrief;

public sealed record GetExperimentBriefQuery(Guid ExperimentId, bool IncludePoints);

public sealed record ExperimentBriefDto(
    Guid Id,
    Guid StrategyId,
    int StrategyVersion,
    string Status,
    string? FailureReason,
    ExperimentConditionsDto Conditions,
    ReplayResultBriefDto? Result,
    IReadOnlyList<ResearchStudyBrief>? Studies);

public sealed record ResearchStudyBrief(
    string Kind,
    ResearchFoldDto Window,
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
        var brief = ReplayResultBrief.Of(report?.Baseline, query.IncludePoints);
        var studies = report?.Studies.Select(BriefOf).ToList();

        return new ExperimentBriefDto(
            Id: experiment.Id,
            StrategyId: experiment.StrategyId,
            StrategyVersion: experiment.StrategyVersion,
            Status: experiment.Status.ToString(),
            FailureReason: experiment.FailureReason,
            Conditions: ExperimentConditionsDto.From(experiment.Conditions),
            Result: brief is null ? null : ReplayResultBriefDto.From(brief),
            Studies: studies);
    }

    private static ResearchStudyBrief BriefOf(ResearchStudy study) => new(
        Kind: study.Kind,
        Window: ResearchFoldDto.From(study.Window),
        ParameterValue: study.ParameterValue,
        Sharpe: study.Summary?.Sharpe,
        AnnualReturn: study.Summary?.AnnualReturn,
        MaxDrawdown: study.Summary?.MaxDrawdown,
        TrainingSharpe: study.TrainingSharpe,
        FailureReason: study.FailureReason);
}
