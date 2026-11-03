using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;

public sealed class ListTrialsHandler(IDraftRepository drafts, ITrialBacktests backtests)
{
    public async Task<Result<IReadOnlyList<TrialRunDto>>> Handle(Guid draftId, CancellationToken cancellationToken)
    {
        var draft = await drafts.FindAsync(draftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(draftId);

        var runs = new List<TrialRunDto>();
        foreach (var run in draft.Trials)
        {
            var summary = await backtests.SummaryOfAsync(run.StrategyId, run.StrategyVersion, cancellationToken);
            runs.Add(TrialMapping.ToDto(run, summary));
        }

        return runs;
    }
}
