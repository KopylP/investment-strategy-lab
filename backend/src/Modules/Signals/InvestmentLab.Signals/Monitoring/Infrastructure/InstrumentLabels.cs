using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class InstrumentLabels(IInstrumentCatalog instruments) : IInstrumentLabels
{
    public async Task<IReadOnlyDictionary<Guid, string>> AllAsync(CancellationToken cancellationToken)
    {
        var catalog = await instruments.GetInstruments(cancellationToken);

        return catalog.ToDictionary(instrument => instrument.Id, instrument => instrument.Label);
    }
}
