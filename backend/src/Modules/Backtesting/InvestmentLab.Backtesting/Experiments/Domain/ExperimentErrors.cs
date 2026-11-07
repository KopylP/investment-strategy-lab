namespace InvestmentLab.Backtesting.Experiments.Domain;

public static class ExperimentErrors
{
    public static Error MissingEngineVersion() =>
        Error.Validation("research.engine_version_missing", "The replay did not identify its strategy engine version.");
    public static Error NotFound(Guid id) =>
        Error.NotFound("research.not_found", "The experiment does not exist.", new { id });

    public static Error InvalidPeriod() =>
        Error.Validation("research.period_invalid", "Choose a historical period of 60 days to 25 years ending no later than today.");

    public static Error InvalidCapital() =>
        Error.Validation("research.capital_invalid", "Capital must be between 100 and 1000000000.");

    public static Error InvalidCosts() =>
        Error.Validation("research.costs_invalid", "Execution costs are outside the allowed range.");

    public static Error InvalidSplit() =>
        Error.Validation("research.split_invalid", "The training and independent test periods are too short.");

    public static Error InvalidWindows() =>
        Error.Validation("research.walk_forward_invalid", "Choose valid walk-forward windows with at least one test period.");

    public static Error TooManyFolds() =>
        Error.Validation("research.too_many_folds", "Use at most six walk-forward test windows per experiment.");

    public static Error InvalidParameter() =>
        Error.Validation("research.parameter_invalid", "Choose one existing numeric parameter and one to five values.");

    public static Error ParametersRefused(IReadOnlyList<string> problems) =>
        Error.Validation("research.parameter_refused", "A candidate violates the strategy parameter rules.", new { problems });

    public static Error ParameterCheckFailed(string? reason) =>
        Error.Validation("research.parameter_check_failed", "The parameter check could not be completed.", new { reason });

    public static Error NotQueued() =>
        Error.Validation("research.not_queued", "Only a queued experiment can be cancelled; this one is running or finished.");

    public static Error NotSupported() =>
        Error.Validation("research.not_supported", "This strategy cannot be replayed.");
}
