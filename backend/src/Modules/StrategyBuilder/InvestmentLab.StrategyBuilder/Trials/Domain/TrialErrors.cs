namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public static class TrialErrors
{
    public static Error DraftHasProblems(object? problems = null) => Error.Conflict("builder.trial.draft_has_problems", "Fix the draft problems before running a trial.", problems);
}
