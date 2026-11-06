using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.GetSignalVocabulary;

public sealed record SignalVocabularyDto(
    IReadOnlyList<string> Targets,
    IReadOnlyList<string> Metrics,
    IReadOnlyList<string> Directions,
    IReadOnlyList<string> Frequencies,
    IReadOnlyList<string> Weekdays);

public static class GetSignalVocabularyHandler
{
    public static SignalVocabularyDto Handle() =>
        new(
            Targets: Enum.GetNames<SignalTargetKind>(),
            Metrics: Enum.GetNames<SignalMetric>(),
            Directions: Enum.GetNames<SignalDirection>(),
            Frequencies: Enum.GetNames<CadenceFrequency>(),
            Weekdays: Enum.GetNames<DayOfWeek>());
}
