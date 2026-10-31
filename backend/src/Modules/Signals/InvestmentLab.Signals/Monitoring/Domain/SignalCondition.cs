namespace InvestmentLab.Signals.Monitoring.Domain;

public sealed record SignalCondition(SignalMetric Metric, SignalDirection Direction, decimal Threshold, decimal RearmAt, int? WindowDays)
{
    private const decimal MaxFall = 1m;
    private const int MinWindowDays = 2;
    private const decimal RecoveryShare = 1m / 6m;

    public static Result<SignalCondition> TryOf(
        SignalMetric metric, SignalDirection direction, decimal threshold, decimal? rearmAt, int? windowDays)
    {
        var effectiveDirection = metric == SignalMetric.Level ? direction : SignalDirection.Above;

        if (ThresholdError(metric, threshold) is { } thresholdError)
            return thresholdError;

        var rearm = rearmAt ?? DefaultRearm(effectiveDirection, threshold);
        if (!RearmIsLessStrict(effectiveDirection, threshold, rearm))
            return SignalErrors.RearmNotBeforeThreshold();

        if (windowDays < MinWindowDays)
            return SignalErrors.WindowTooShort();

        var window = metric == SignalMetric.DrawdownFromPeak ? windowDays : null;

        return new SignalCondition(metric, effectiveDirection, threshold, decimal.Round(rearm, 6), window);
    }

    public bool IsCrossedBy(decimal value) =>
        Direction == SignalDirection.Above ? value >= Threshold : value <= Threshold;

    public bool IsRearmedBy(decimal value) =>
        Direction == SignalDirection.Above ? value <= RearmAt : value >= RearmAt;

    private static Error? ThresholdError(SignalMetric metric, decimal threshold)
    {
        if (threshold <= 0m)
            return metric == SignalMetric.Level ? SignalErrors.LevelThresholdOutOfRange() : SignalErrors.FallThresholdOutOfRange();

        if (metric != SignalMetric.Level && threshold > MaxFall)
            return SignalErrors.FallThresholdOutOfRange();

        return null;
    }

    private static decimal DefaultRearm(SignalDirection direction, decimal threshold)
    {
        var recovery = threshold * RecoveryShare;

        return direction == SignalDirection.Above ? threshold - recovery : threshold + recovery;
    }

    private static bool RearmIsLessStrict(SignalDirection direction, decimal threshold, decimal rearm) =>
        direction == SignalDirection.Above ? rearm < threshold : rearm > threshold;
}
