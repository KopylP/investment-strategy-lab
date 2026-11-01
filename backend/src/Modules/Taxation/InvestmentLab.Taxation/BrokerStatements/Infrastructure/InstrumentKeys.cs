using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Taxation.BrokerStatements.Domain;

namespace InvestmentLab.Taxation.BrokerStatements.Infrastructure;

internal sealed class InstrumentKeys(IInstrumentCatalog instruments) : IInstrumentKeys
{
    public async Task<IReadOnlyDictionary<Guid, InstrumentKey>> AllAsync(CancellationToken cancellationToken)
    {
        var known = await instruments.GetInstruments(cancellationToken);

        return known.ToDictionary(instrument => instrument.Id, InstrumentKeyOf);
    }

    private static InstrumentKey InstrumentKeyOf(InstrumentDto instrument) =>
        new(instrument.Isin, instrument.Ticker is { } ticker ? [ticker] : []);
}
