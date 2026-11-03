using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.CreateDraft;

public sealed record CreateDraftCommand(string Name, string? Template);

public sealed class CreateDraftHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock)
{
    public async Task<Result<DraftDto>> Handle(UserId owner, CreateDraftCommand command, CancellationToken cancellationToken)
    {
        if (command.Template is { } template && !DraftTemplates.IsKnown(template))
            return DraftErrors.TemplateUnknown(template);

        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var graph = command.Template is { } chosen ? DraftTemplates.GraphOf(chosen) : StrategyGraph.Empty;
        var reports = DraftReports.DefaultsOf(vocabulary.Value.Reports);
        var started = StrategyDraft.Start(owner, command.Name ?? "", DraftSetup.Empty, graph, reports, null, clock.GetUtcNow());
        if (started.IsFailure)
            return started.Error;

        drafts.Add(started.Value);
        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(started.Value, cancellationToken);
    }
}
