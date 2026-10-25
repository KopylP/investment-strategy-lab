using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application.GetInstruments;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class InstrumentCatalog(GetInstrumentsHandler getInstruments) : IInstrumentCatalog
{
    public Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken) =>
        getInstruments.Handle(cancellationToken);
}
