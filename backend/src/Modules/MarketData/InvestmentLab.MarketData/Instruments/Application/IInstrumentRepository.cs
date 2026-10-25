using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application;

public interface IInstrumentRepository
{
    Task<Instrument?> FindAsync(InstrumentId id, CancellationToken cancellationToken);

    Task<bool> IsinTakenAsync(Isin isin, CancellationToken cancellationToken);

    /// <summary>The instrument that owns the symbol, if any.</summary>
    Task<InstrumentId?> FindOwnerAsync(ProviderSymbol symbol, CancellationToken cancellationToken);

    Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken cancellationToken);

    void Add(Instrument instrument);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
