using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.GetSignals;

public sealed record SignalsOverviewDto(
    IReadOnlyList<SignalDto> Signals,
    IReadOnlyList<SignalOccurrenceDto> Active,
    IReadOnlyList<SignalOccurrenceDto> Recent);

public sealed class GetSignalsHandler(ISignalRepository signals)
{
    private const int RecentCount = 30;

    public async Task<Result<SignalsOverviewDto>> Handle(CancellationToken cancellationToken)
    {
        var all = await signals.AllAsync(cancellationToken);
        var open = await signals.AllOpenOccurrencesAsync(cancellationToken);
        var cleared = await signals.ClearedOccurrencesAsync(RecentCount, cancellationToken);

        var signalsById = all.ToDictionary(signal => signal.Id);
        var newestOpenFirst = open.OrderByDescending(occurrence => occurrence.TriggeredAt);

        return new SignalsOverviewDto(
            Signals: MostActiveFirst(all, open),
            Active: ToOccurrenceDtos(newestOpenFirst, signalsById),
            Recent: ToOccurrenceDtos(cleared, signalsById));
    }

    private static List<SignalDto> MostActiveFirst(IReadOnlyList<Signal> all, IReadOnlyList<SignalOccurrence> open)
    {
        var activeCounts = open.CountBy(occurrence => occurrence.SignalId).ToDictionary();

        return [.. all
            .Select(signal => SignalDto.Of(signal, activeCounts.GetValueOrDefault(signal.Id)))
            .OrderByDescending(signal => signal.ActiveCount)
            .ThenBy(signal => signal.Name, StringComparer.CurrentCulture)];
    }

    private static List<SignalOccurrenceDto> ToOccurrenceDtos(IEnumerable<SignalOccurrence> occurrences, Dictionary<Guid, Signal> signalsById) =>
        [.. occurrences
            .Where(occurrence => signalsById.ContainsKey(occurrence.SignalId))
            .Select(occurrence => SignalOccurrenceDto.Of(occurrence, signalsById[occurrence.SignalId]))];
}
