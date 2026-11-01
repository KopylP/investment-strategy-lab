namespace InvestmentLab.Signals.Monitoring.Domain;

public static class SignalErrors
{
    public static Error NotFound(Guid signalId) =>
        Error.NotFound("signal.not_found", $"Signal {signalId} does not exist.");

    public static Error InvalidName() =>
        Error.Validation("signal.name", $"A signal name has 1 to {Signal.MaxNameLength} characters.", new { maximum = Signal.MaxNameLength });

    public static Error UnknownTarget() =>
        Error.Validation("signal.target", "Choose what the signal watches.");

    public static Error UnknownMetric() =>
        Error.Validation("signal.metric", "Choose the metric of the signal.");

    public static Error MetricDoesNotFitTarget() =>
        Error.Validation("signal.metric_unfit", "This metric does not fit this target.");

    public static Error FallThresholdOutOfRange() =>
        Error.Validation("signal.threshold", "A fall threshold is above 0% and at most 100%.");

    public static Error LevelThresholdOutOfRange() =>
        Error.Validation("signal.threshold_level", "A level threshold is above zero.");

    public static Error RearmNotBeforeThreshold() =>
        Error.Validation("signal.rearm", "The rearm level is less strict than the threshold.");

    public static Error WindowTooShort() =>
        Error.Validation("signal.window", "The peak window is longer than one day.");

    public static Error AccountNotFound() =>
        Error.NotFound("signal.account", "The account does not exist.");

    public static Error SubPortfolioNotFound() =>
        Error.NotFound("signal.sub_portfolio", "The account has no such sub-portfolio.");

    public static Error InstrumentNotFound() =>
        Error.NotFound("signal.instrument", "The instrument does not exist.");

    public static Error BuiltInCannotBeDeleted() =>
        Error.Conflict("signal.built_in", "A built-in signal cannot be deleted, only turned off.");
}
