using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;

public sealed record RunTrialCommand(Guid DraftId, string? Label);

public sealed class RunTrialHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, IStrategyAuthoring authoring, ITrialBacktests backtests, ITrialRunGate gate, TimeProvider clock)
{
    public async Task<Result<TrialRunDto>> Handle(RunTrialCommand command, CancellationToken cancellationToken)
    {
        await using var lease = await gate.EnterAsync(cancellationToken);

        var draft = await drafts.FindAsync(command.DraftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(command.DraftId);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var problems = GraphCheck.Of(draft, vocabulary.Value);
        if (DraftCheck.HasErrors(problems))
            return ProblemsOf(problems);

        var rules = RulesGenerator.Generate(draft);
        var fingerprint = DraftFingerprint.Of(rules, draft.Setup, draft.Reports);
        if (draft.TrialWith(fingerprint) is { } existing)
            return await DescribeAsync(existing, cancellationToken);

        var strategy = new AuthoredStrategy(rules.Json, draft.Setup, draft.Reports);
        var written = await authoring.WriteTrialVersionAsync(draft.OwnerId, draft.TrialStrategyId, strategy, cancellationToken);
        if (written.IsFailure)
        {
            if (written.Error.Parameters is not RulesRefused refused)
                return written.Error;

            var placed = EngineProblems.Placed(refused.Problems, rules.SourceMap, draft.Graph);

            return ProblemsOf(placed);
        }

        var version = written.Value;
        var run = draft.RecordTrial(version.StrategyId, version.Version, fingerprint, command.Label, clock.GetUtcNow());
        await drafts.SaveChangesAsync(cancellationToken);

        return await DescribeAsync(run, cancellationToken);
    }

    private static Error ProblemsOf(IReadOnlyList<DraftProblem> problems)
    {
        var placed = problems.Select(problem => new TrialProblemDto(
            Message: problem.Message,
            Severity: problem.Severity.ToString(),
            NodeId: problem.Node?.Value)).ToList();

        return TrialErrors.DraftHasProblems(new { problems = placed });
    }

    private async Task<TrialRunDto> DescribeAsync(TrialRun run, CancellationToken cancellationToken)
    {
        var summary = await backtests.SummaryOfAsync(run.StrategyId, run.StrategyVersion, cancellationToken);

        return TrialMapping.ToDto(run, summary);
    }
}
