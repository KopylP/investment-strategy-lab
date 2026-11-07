using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;

public sealed record RunTrialCommand(Guid DraftId, string? Label, TrialExperiment? Experiment = null, bool Rerun = false);

public sealed class RunTrialHandler(
    IDraftRepository drafts,
    IDraftChecks checker,
    IStrategyAuthoring authoring,
    ITrialExperiments experiments,
    TrialRunSummaries summaries,
    ITrialRunGate gate,
    TimeProvider clock)
{
    public async Task<Result<TrialRunDto>> Handle(RunTrialCommand command, CancellationToken cancellationToken)
    {
        await using var lease = await gate.EnterAsync(cancellationToken);

        var draft = await drafts.FindAsync(command.DraftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(command.DraftId);

        var checkedDraft = await checker.CheckAsync(draft, cancellationToken);
        if (checkedDraft.IsFailure)
            return checkedDraft.Error;
        if (!checkedDraft.Value.IsClean)
            return ProblemsOf(checkedDraft.Value.Problems);

        var rules = checkedDraft.Value.Rules!;
        var experimentCheck = await CheckExperiment(command.Experiment, rules, cancellationToken);
        if (experimentCheck.IsFailure)
            return experimentCheck.Error;

        var fingerprint = DraftFingerprint.Of(rules, draft.Setup, draft.Reports, command.Experiment);
        if (!command.Rerun && await ReusableTrialAsync(draft, fingerprint, cancellationToken) is { } reused)
            return reused;

        var executed = await Execute(draft, rules, command.Experiment, cancellationToken);
        if (executed.IsFailure)
            return executed.Error;

        var run = draft.RecordTrial(executed.Value, fingerprint, command.Label, clock.GetUtcNow());
        await drafts.SaveChangesAsync(cancellationToken);

        return await summaries.DescribeAsync(run, cancellationToken);
    }

    private async Task<TrialRunDto?> ReusableTrialAsync(StrategyDraft draft, string fingerprint, CancellationToken cancellationToken)
    {
        if (draft.TrialWith(fingerprint) is not { } existing)
            return null;

        var described = await summaries.DescribeAsync(existing, cancellationToken);

        return described.Status == nameof(TrialStatus.Failed) ? null : described;
    }

    private async Task<Result> CheckExperiment(TrialExperiment? experiment, GeneratedRules rules, CancellationToken cancellationToken)
    {
        if (experiment is null)
            return Result.Success();
        if (experiment.ShapeProblem() is { } shapeProblem)
            return Result.Failure(shapeProblem);

        return await experiments.CheckAsync(experiment, rules.Json, cancellationToken);
    }

    private async Task<Result<TrialExecution>> Execute(
        StrategyDraft draft,
        GeneratedRules rules,
        TrialExperiment? experiment,
        CancellationToken cancellationToken)
    {
        var setup = SetupBindings.EvaluationSetup(draft);
        var strategy = new AuthoredStrategy(rules.Json, setup, draft.Reports);
        var written = await authoring.WriteTrialVersionAsync(draft.OwnerId, draft.TrialStrategyId, strategy, cancellationToken);
        if (written.IsFailure)
            return AuthoringFailure(written.Error, rules, draft.Graph);

        if (experiment is null)
            return new TrialExecution(written.Value);

        var requested = await experiments.RequestAsync(written.Value, experiment, cancellationToken);
        if (requested.IsFailure)
            return requested.Error;

        return new TrialExecution(written.Value, requested.Value, experiment);
    }

    private static Error AuthoringFailure(Error error, GeneratedRules rules, StrategyGraph graph)
    {
        if (error.Parameters is not RulesRefused refused)
            return error;

        var placed = EngineProblems.Placed(refused.Problems, rules.SourceMap, graph);

        return ProblemsOf(placed);
    }

    private static Error ProblemsOf(IReadOnlyList<DraftProblem> problems)
    {
        var placed = problems.Select(ProblemOf).ToList();

        return TrialErrors.DraftHasProblems(new { problems = placed });
    }

    private static TrialProblemDto ProblemOf(DraftProblem problem) => new(
        Message: problem.Message,
        Severity: problem.Severity.ToString(),
        NodeId: problem.Node?.Value);
}
