using InvestmentLab.MarketData.Contracts.Instruments;

namespace InvestmentLab.MarketData.Instruments.Application.GetInstruments;

public sealed class GetInstrumentsHandler(IInstrumentRepository instruments)
{
    public async Task<IReadOnlyList<InstrumentDto>> Handle(CancellationToken cancellationToken) =>
        [.. (await instruments.ListAsync(cancellationToken)).Select(instrument => instrument.ToDto())];
}
