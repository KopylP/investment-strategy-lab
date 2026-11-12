using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.GetDraftRules;

public sealed record DraftRulesDto(string Json);

public sealed class GetDraftRulesHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, IOwnSignals signals) : IHandler
{
    public async Task<Result<DraftRulesDto>> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;
        if (DraftCheck.HasErrors(GraphCheck.Of(draft, vocabulary.Value, await signals.AllAsync(cancellationToken))))
            return DraftErrors.HasProblems();

        return new DraftRulesDto(RulesGenerator.Generate(draft).Json);
    }
}
