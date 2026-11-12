using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ReplaceDraftRules;

public sealed record ReplaceDraftRulesCommand(int Revision, string Json);

public sealed class ReplaceDraftRulesHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock) : IHandler
{
    public async Task<Result<DraftDto>> Handle(Guid draftId, ReplaceDraftRulesCommand command, CancellationToken cancellationToken)
    {
        if (await drafts.FindAsync(draftId, cancellationToken) is not { } draft)
            return DraftErrors.NotFound(draftId);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var imported = GraphFromRules.Build(command.Json ?? "", vocabulary.Value);
        if (imported.IsFailure)
            return imported.Error;

        var change = new DraftChange(
            Revision: command.Revision,
            Name: null,
            Description: null,
            Setup: draft.Setup with { ExtraSeries = imported.Value.ExtraSeries },
            Graph: imported.Value.Graph,
            Reports: null);
        var changed = draft.Change(change, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(draft, cancellationToken);
    }
}
