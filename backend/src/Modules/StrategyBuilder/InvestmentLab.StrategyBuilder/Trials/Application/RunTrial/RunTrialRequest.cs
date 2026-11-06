using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;

public sealed record RunTrialRequest(string? Label, TrialExperiment? Experiment = null)
{
    public RunTrialCommand CommandFor(Guid draftId) => new(draftId, Label, Experiment);
}

