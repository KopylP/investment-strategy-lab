using System.Globalization;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.Integration;

internal sealed class SignalFiredMapper : IDomainEventMapper<SignalFired, SignalTriggeredV1>
{
    private static readonly CultureInfo Ukrainian = CultureInfo.GetCultureInfo("uk-UA");

    public SignalTriggeredV1 Map(SignalFired fired)
    {
        var newestData = fired.Subjects.Max(reading => reading.DataDate);
        var subjects = fired.Subjects.Select(reading => SubjectOf(fired.Condition, reading)).ToList();

        return new SignalTriggeredV1(
            TriggerId: fired.TriggerId,
            SignalId: fired.SignalId,
            OwnerId: fired.OwnerId.Value,
            AccountId: fired.AccountId,
            Name: fired.SignalName,
            Metric: fired.Condition.Metric.ToString(),
            DataDate: newestData,
            TriggeredAt: fired.OccurredAt,
            Subjects: subjects);
    }

    private static TriggeredSubjectDto SubjectOf(SignalCondition condition, SubjectReading reading)
    {
        var text = Message.Of("signal.subject.reading", new { label = reading.Label, value = ValueText(condition, reading.Value) });

        return new TriggeredSubjectDto(reading.Key, reading.Label, reading.Value, text);
    }

    private static string ValueText(SignalCondition condition, decimal value)
    {
        if (condition.Metric == SignalMetric.Level)
            return value.ToString("0.##", Ukrainian);

        var percent = value * 100m;

        return $"−{percent.ToString("0.#", Ukrainian)}%";
    }
}
