using InvestmentLab.Signals.Contracts;
using InvestmentLab.Signals.Monitoring.Application.ReadSubjects;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class MarketSignalStates(ISignalRepository signals) : IMarketSignalStates
{
    public async Task<IReadOnlyList<MarketSignalStateDto>> GetStatesAsync(IReadOnlyList<Guid> signalIds, CancellationToken cancellationToken)
    {
        var all = await signals.AllAsync(cancellationToken);
        var market = all.Where(signal => signal.Target.IsMarket && signalIds.Contains(signal.Id)).ToList();
        var ids = market.Select(signal => signal.Id).ToList();
        var occurrences = await signals.OpenOccurrencesOfAsync(ids, cancellationToken);

        return [.. market.Select(signal => new MarketSignalStateDto(signal.Id, ActiveFor(signal.Id, occurrences)))];
    }

    private static IReadOnlyList<Guid> ActiveFor(Guid signalId, IReadOnlyList<SignalOccurrence> occurrences)
    {
        var subjects = occurrences.Where(occurrence => occurrence.SignalId == signalId);
        var instruments = subjects.Select(occurrence => InstrumentSubjects.InstrumentIdOf(occurrence.SubjectKey));

        return [.. instruments.OfType<Guid>().Distinct()];
    }
}
