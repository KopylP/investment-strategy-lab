namespace InvestmentLab.MarketData.Contracts.Instruments;

public interface IInstrumentCatalog
{
    Task<IReadOnlyList<InstrumentDto>> GetInstruments(CancellationToken cancellationToken);
}
