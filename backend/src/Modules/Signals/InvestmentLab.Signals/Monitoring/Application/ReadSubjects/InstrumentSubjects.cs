namespace InvestmentLab.Signals.Monitoring.Application.ReadSubjects;

internal static class InstrumentSubjects
{
    private const int ShortIdLength = 8;

    public static string KeyOf(Guid instrumentId) => $"instrument:{instrumentId:N}";

    public static string LabelOf(IReadOnlyDictionary<Guid, string> labels, Guid instrumentId) =>
        labels.TryGetValue(instrumentId, out var label) ? label : instrumentId.ToString("N")[..ShortIdLength];
}
