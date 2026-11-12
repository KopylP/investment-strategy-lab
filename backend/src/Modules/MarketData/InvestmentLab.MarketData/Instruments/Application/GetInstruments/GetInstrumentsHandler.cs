using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.GetInstruments;

public sealed class GetInstrumentsHandler(IInstrumentRepository instruments) : IHandler
{
    public async Task<IReadOnlyList<InstrumentDto>> Handle(CancellationToken cancellationToken) =>
        [.. (await instruments.ListAsync(cancellationToken)).Select(instrument => instrument.ToDto())];
}
