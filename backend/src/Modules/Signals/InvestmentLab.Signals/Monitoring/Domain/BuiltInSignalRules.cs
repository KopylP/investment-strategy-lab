namespace InvestmentLab.Signals.Monitoring.Domain;

public static class BuiltInSignalRules
{
    private const decimal VixLevelThreshold = 30m;
    private const decimal AccountFallThreshold = 0.30m;
    private const decimal PositionFallThreshold = 0.20m;
    private const int PositionPeakWindowDays = 365;

    public static SignalCondition ConditionOf(string kind) => kind switch
    {
        BuiltInSignalKind.VolatilityIndex => ConditionAbove(SignalMetric.Level, VixLevelThreshold, windowDays: null),
        BuiltInSignalKind.AccountDrawdown => ConditionAbove(SignalMetric.DrawdownFromPeak, AccountFallThreshold, windowDays: null),
        BuiltInSignalKind.PositionDrawdown => ConditionAbove(SignalMetric.DrawdownFromPeak, PositionFallThreshold, PositionPeakWindowDays),
        _ => throw UnknownKind(kind),
    };

    public static SignalTargetKind TargetOf(string kind) => kind switch
    {
        BuiltInSignalKind.VolatilityIndex => SignalTargetKind.Instrument,
        BuiltInSignalKind.AccountDrawdown => SignalTargetKind.Account,
        BuiltInSignalKind.PositionDrawdown => SignalTargetKind.EachPosition,
        _ => throw UnknownKind(kind),
    };

    private static SignalCondition ConditionAbove(SignalMetric metric, decimal threshold, int? windowDays) =>
        SignalCondition.TryOf(metric, SignalDirection.Above, threshold, rearmAt: null, windowDays).Value;

    private static ArgumentException UnknownKind(string kind) =>
        new($"'{kind}' is not a known kind of built-in signal.", nameof(kind));
}
