using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Application.ImportRules;

public sealed record ImportRulesCommand(string Name, string Json);

public sealed class ImportRulesHandler(IDraftRepository drafts, IRuleVocabulary vocabularies, DraftDescriber describer, TimeProvider clock) : IHandler
{
    public async Task<Result<DraftDto>> Handle(UserId owner, ImportRulesCommand command, CancellationToken cancellationToken)
    {
        var vocabulary = await vocabularies.GetAsync(cancellationToken);
        if (vocabulary.IsFailure)
            return vocabulary.Error;

        var imported = GraphFromRules.Build(command.Json ?? "", vocabulary.Value);
        if (imported.IsFailure)
            return imported.Error;

        var setup = DraftSetup.Empty with { ExtraSeries = imported.Value.ExtraSeries };
        var reports = DraftReports.DefaultsOf(vocabulary.Value.Reports);
        var started = StrategyDraft.Start(owner, command.Name ?? "", setup, imported.Value.Graph, reports, null, clock.GetUtcNow());
        if (started.IsFailure)
            return started.Error;

        drafts.Add(started.Value);
        await drafts.SaveChangesAsync(cancellationToken);

        return await describer.DescribeAsync(started.Value, cancellationToken);
    }
}
