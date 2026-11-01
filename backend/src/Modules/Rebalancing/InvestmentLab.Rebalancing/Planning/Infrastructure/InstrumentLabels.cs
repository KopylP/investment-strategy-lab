using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class InstrumentLabels(IInstrumentCatalog instruments) : IInstrumentLabels
{
    public async Task<IReadOnlyDictionary<Guid, string>> AllAsync(CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);

        return known.ToDictionary(instrument => instrument.Id, instrument => instrument.Label);
    }
}
