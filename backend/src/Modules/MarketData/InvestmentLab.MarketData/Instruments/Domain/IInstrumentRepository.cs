namespace InvestmentLab.MarketData.Instruments.Domain;

public interface IInstrumentRepository
{
    Task<Instrument?> FindAsync(InstrumentId id, CancellationToken cancellationToken);

    Task<bool> IsinTakenAsync(Isin isin, CancellationToken cancellationToken);

    Task<InstrumentId?> FindOwnerAsync(ProviderSymbol symbol, CancellationToken cancellationToken);

    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Instrument>> ListForUpdateAsync(CancellationToken cancellationToken);

    void Add(Instrument instrument);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
