using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.CancelTrial;

public sealed class CancelTrialHandler(IDraftRepository drafts, ITrialExperiments experiments) : IHandler
{
    public async Task<Result> Handle(Guid draftId, int number, CancellationToken cancellationToken)
    {
        var draft = await drafts.FindAsync(draftId, cancellationToken);
        if (draft is null)
            return DraftErrors.NotFound(draftId);

        var trial = draft.Trials.FirstOrDefault(run => run.Number == number);
        if (trial is null)
            return TrialErrors.NotFound(number);
        if (trial.ExperimentId is not { } experimentId)
            return TrialErrors.NotCancellable(number);

        return await experiments.CancelAsync(experimentId, cancellationToken);
    }
}
