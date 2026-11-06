namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public static class TrialErrors
{
    public static Error ExperimentConditionsMissing() =>
        Error.Validation("research.conditions_missing", "The experiment needs a period, costs and a robustness plan.");

    public static Error ExperimentRefused(string reason) =>
        Error.Validation("research.request_refused", "The experiment request was refused.", new { reason });

    public static Error DraftHasProblems(object? problems = null) =>
        Error.Conflict("builder.trial.draft_has_problems", "Fix the draft problems before running a trial.", problems);
}
