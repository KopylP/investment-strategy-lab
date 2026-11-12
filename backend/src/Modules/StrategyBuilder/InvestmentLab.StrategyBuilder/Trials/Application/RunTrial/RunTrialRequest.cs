using InvestmentLab.StrategyBuilder.Trials.Application.Shared;

namespace InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;

public sealed record RunTrialRequest(string? Label, TrialExperimentDto? Experiment = null, bool Rerun = false)
{
    public RunTrialCommand CommandFor(Guid draftId) => new(draftId, Label, Experiment, Rerun);
}

