namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public static class TrialErrors
{
    public static Error ExperimentConditionsMissing() =>
        Error.Validation("research.conditions_missing", "The experiment needs a period, costs and a robustness plan.");

    public static Error ExperimentRefused(string reason) =>
        Error.Validation("research.request_refused", "The experiment request was refused.", new { reason });

    public static Error NotFound(int number) =>
        Error.NotFound("builder.trial.not_found", "The draft has no trial with this number.", new { number });

    public static Error NotCancellable(int number) =>
        Error.Validation("builder.trial.not_cancellable", "Only a trial with experiment conditions waits in the queue and can be cancelled.", new { number });

    public static Error DraftHasProblems(object? problems = null) =>
        Error.Conflict("builder.trial.draft_has_problems", "Fix the draft problems before running a trial.", problems);
}
