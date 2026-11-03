using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.Shared;

public sealed class DraftChecker(IRuleVocabulary vocabularies, IStrategyAuthoring authoring)
{
    public async Task<Result<DraftCheck>> CheckAsync(StrategyDraft draft, CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var local = GraphCheck.Of(draft, vocabulary.Value);
        if (DraftCheck.HasErrors(local))
            return new DraftCheck(local, null, []);

        var rules = RulesGenerator.Generate(draft);
        var engine = await authoring.CheckAsync(rules.Json, cancellationToken);
        if (engine.IsFailure)
            return engine.Error;

        var placed = EngineProblems.Placed(engine.Value.Problems, rules.SourceMap, draft.Graph);

        return new DraftCheck([.. local, .. placed], rules, engine.Value.SignalIds);
    }
}
