using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.GetSignals;

public sealed record SignalOccurrenceDto(
    Guid Id,
    Guid SignalId,
    string SignalName,
    string Metric,
    string SubjectLabel,
    decimal TriggerValue,
    decimal LatestValue,
    DateOnly TriggerDate,
    DateOnly LatestDate,
    DateTimeOffset TriggeredAt,
    DateTimeOffset? ClearedAt,
    string? ClearReason)
{
    public static SignalOccurrenceDto Of(SignalOccurrence occurrence, Signal signal) => new(
        Id: occurrence.Id,
        SignalId: signal.Id,
        SignalName: signal.Name,
        Metric: signal.Condition.Metric.ToString(),
        SubjectLabel: occurrence.SubjectLabel,
        TriggerValue: occurrence.TriggerValue,
        LatestValue: occurrence.LatestValue,
        TriggerDate: occurrence.TriggerDate,
        LatestDate: occurrence.LatestDate,
        TriggeredAt: occurrence.TriggeredAt,
        ClearedAt: occurrence.ClearedAt,
        ClearReason: occurrence.ClearReason?.ToString());
}
