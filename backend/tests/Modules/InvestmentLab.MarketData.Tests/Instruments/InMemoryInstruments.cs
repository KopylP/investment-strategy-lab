using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

internal sealed class InMemoryInstruments : IInstrumentRepository
{
    public List<Instrument> Items { get; } = [];

    public int Saves { get; private set; }

    public Task<Instrument?> FindAsync(InstrumentId id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

    public Task<bool> IsinTakenAsync(Isin isin, CancellationToken cancellationToken) =>
        Task.FromResult(Items.Any(i => i.Isin == isin));

    public Task<InstrumentId?> FindOwnerAsync(ProviderSymbol symbol, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(i => i.ProviderSymbols.Contains(symbol))?.Id);

    public Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Instrument>>(Items);

    public Task<IReadOnlyList<Instrument>> ListForUpdateAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Instrument>>(Items);

    public void Add(Instrument instrument) => Items.Add(instrument);

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        Saves++;

        return Task.CompletedTask;
    }
}
