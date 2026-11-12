using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.RunExperiment;

public sealed class RunExperimentHandler(
    IExperimentRepository experiments,
    IResearchStrategies strategies,
    IResearchReplay replay,
    TimeProvider clock) : IHandler
{
    public async Task Handle(Guid experimentId, CancellationToken cancellationToken)
    {
        var experiment = await experiments.FindAsync(experimentId, cancellationToken);
        if (experiment is null)
            return;

        var started = experiment.Start();
        if (started.IsFailure)
            return;

        await experiments.SaveAsync(cancellationToken);

        var result = await Run(experiment, cancellationToken);
        var now = clock.GetUtcNow();

        if (result.IsSuccess)
            experiment.Complete(result.Value, now);
        else
            experiment.Fail(result.Error.AsMessage(), now);

        await experiments.SaveAsync(cancellationToken);
    }

    private async Task<Result<ResearchReport>> Run(ResearchExperiment experiment, CancellationToken cancellationToken)
    {
        var planned = await Plan(experiment, cancellationToken);
        if (planned.IsFailure)
            return planned.Error;

        var plan = planned.Value;
        var setup = plan.ReplayFor(plan.Baseline, plan.Conditions.Period, journal: true);
        var baseline = await replay.RunAsync(setup, cancellationToken);
        if (baseline.IsFailure)
            return baseline.Error;

        var fullRuns = new Dictionary<ParameterVariant, ReplayResult> { [plan.Baseline] = baseline.Value };
        var sensitivity = await Sensitivity(plan, fullRuns, cancellationToken);
        var independent = await TestWindows(plan, plan.IndependentTests, ResearchStudyKinds.OutOfSample, fullRuns, cancellationToken);
        var walkForward = await TestWindows(plan, plan.WalkForwardTests, ResearchStudyKinds.WalkForward, fullRuns, cancellationToken);
        var studies = sensitivity.Concat(independent).Concat(walkForward).ToList();

        return plan.ReportOf(baseline.Value, studies);
    }

    private async Task<Result<ResearchPlan>> Plan(ResearchExperiment experiment, CancellationToken cancellationToken)
    {
        var source = await strategies.FindAsync(experiment.StrategyId, experiment.StrategyVersion, cancellationToken);
        if (source is null)
            return BacktestErrors.StrategyNotFound(experiment.StrategyId);

        var conditions = experiment.Conditions;
        var candidates = ParameterVariants.Of(source.Parameters, conditions.Robustness.Neighborhood);
        if (candidates.IsFailure)
            return candidates.Error;

        var history = await HistoryRequirements(source, candidates.Value, cancellationToken);
        if (history.IsFailure)
            return history.Error;

        return new ResearchPlan(source, conditions, candidates.Value, history.Value);
    }

    private async Task<Result<IReadOnlyList<int>>> HistoryRequirements(
        ResearchSource source,
        IReadOnlyList<ParameterVariant> candidates,
        CancellationToken cancellationToken)
    {
        var requirements = new List<int>();

        foreach (var candidate in candidates)
        {
            var check = await strategies.CheckAsync(source.TypeCode, candidate.Parameters, cancellationToken);
            if (check.IsFailure)
                return check.Error;

            requirements.Add(check.Value);
        }

        return requirements;
    }

    private async Task<IReadOnlyList<ResearchStudy>> Sensitivity(
        ResearchPlan plan,
        Dictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        var studies = new List<ResearchStudy>();
        var window = new ResearchFold(Training: null, Test: plan.Conditions.Period);

        foreach (var candidate in plan.SensitivityVariants)
        {
            var result = await FullRunOf(plan, candidate, fullRuns, cancellationToken);

            studies.Add(StudyOf(ResearchStudyKinds.Sensitivity, window, candidate.Value, result));
        }

        return studies;
    }

    private async Task<Result<ReplayResult>> FullRunOf(
        ResearchPlan plan,
        ParameterVariant candidate,
        Dictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        if (fullRuns.TryGetValue(candidate, out var known))
            return known;

        var setup = plan.ReplayFor(candidate, plan.Conditions.Period, journal: false);
        var result = await replay.RunAsync(setup, cancellationToken);
        if (result.IsSuccess)
            fullRuns[candidate] = result.Value;

        return result;
    }

    private async Task<IReadOnlyList<ResearchStudy>> TestWindows(
        ResearchPlan plan,
        IReadOnlyList<ResearchFold> windows,
        string kind,
        IReadOnlyDictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        var studies = new List<ResearchStudy>();

        foreach (var window in windows)
        {
            var study = await TestWindow(plan, window, kind, fullRuns, cancellationToken);
            studies.Add(study);
        }

        return studies;
    }

    private async Task<ResearchStudy> TestWindow(
        ResearchPlan plan,
        ResearchFold window,
        string kind,
        IReadOnlyDictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        var training = await TrainingResults(plan, window.Training!, fullRuns, cancellationToken);
        var selected = TrainingSelection.HighestSharpe(training);
        if (selected is null)
            return FailedTraining(kind, window);

        var setup = plan.ReplayFor(selected.Candidate, window.Test, journal: false);
        var result = await replay.RunAsync(setup, cancellationToken);
        var study = StudyOf(kind, window, selected.Candidate.Value, result);

        return study with { TrainingSharpe = selected.Summary.Sharpe };
    }

    private async Task<IReadOnlyList<TrainingResult>> TrainingResults(
        ResearchPlan plan,
        ExperimentPeriod period,
        IReadOnlyDictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        var results = new List<TrainingResult>();

        foreach (var candidate in plan.Variants)
        {
            var summary = await TrainingSummaryOf(plan, candidate, period, fullRuns, cancellationToken);
            if (summary is not null)
                results.Add(new TrainingResult(candidate, summary));
        }

        return results;
    }

    private async Task<ReplaySummary?> TrainingSummaryOf(
        ResearchPlan plan,
        ParameterVariant candidate,
        ExperimentPeriod period,
        IReadOnlyDictionary<ParameterVariant, ReplayResult> fullRuns,
        CancellationToken cancellationToken)
    {
        if (plan.TrainingReadFromFullRuns(period) && CutOf(fullRuns, candidate, period) is { } cut)
            return cut;

        var setup = plan.ReplayFor(candidate, period, journal: false);
        var result = await replay.RunAsync(setup, cancellationToken);

        return result.IsSuccess ? result.Value.StrategySummary : null;
    }

    private static ReplaySummary? CutOf(
        IReadOnlyDictionary<ParameterVariant, ReplayResult> fullRuns,
        ParameterVariant candidate,
        ExperimentPeriod period)
    {
        var trace = fullRuns.GetValueOrDefault(candidate)?.Trace;

        return trace?.SummaryBetween(period.Start, period.End);
    }

    private static ResearchStudy StudyOf(
        string kind,
        ResearchFold window,
        decimal? parameterValue,
        Result<ReplayResult> result)
    {
        if (result.IsFailure)
            return new ResearchStudy(kind, window, parameterValue, null, FailureReason: result.Error.AsMessage());

        var observedPeriod = new ExperimentPeriod(result.Value.Start, result.Value.End);
        var observedWindow = window with { Test = observedPeriod };

        return new ResearchStudy(kind, observedWindow, parameterValue, result.Value.StrategySummary);
    }

    private static ResearchStudy FailedTraining(string kind, ResearchFold window) =>
        new(
            Kind: kind,
            Window: window,
            ParameterValue: null,
            Summary: null,
            FailureReason: Message.Of("research.training_failed"));
}
