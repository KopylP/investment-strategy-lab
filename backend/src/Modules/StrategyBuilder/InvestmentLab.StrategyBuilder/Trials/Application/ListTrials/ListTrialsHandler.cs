using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;

namespace InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;

public sealed class ListTrialsHandler(IDraftRepository drafts, TrialRunSummaries summaries)
{
    public async Task<Result<IReadOnlyList<TrialRunDto>>> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await drafts.FindAsync(draftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(draftId);

        var runs = new List<TrialRunDto>();
        foreach (var run in draft.Trials)
        {
            var described = await summaries.DescribeAsync(run, cancellationToken);
            runs.Add(described);
        }

        return runs;
    }
}
