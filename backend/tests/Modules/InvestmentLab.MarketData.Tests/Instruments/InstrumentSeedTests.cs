using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Instruments.Infrastructure.Seed;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class InstrumentSeedTests
{
    private sealed class InMemoryInstruments : IInstrumentRepository
    {
        public List<Instrument> Items { get; } = [];

        public Task<Instrument?> FindAsync(InstrumentId id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<bool> IsinTakenAsync(Isin isin, CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(i => i.Isin == isin));

        public Task<InstrumentId?> FindOwnerAsync(ProviderSymbol symbol, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(i => i.ProviderSymbols.Contains(symbol))?.Id);

        public Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Instrument>>(Items);

        public void Add(Instrument instrument) => Items.Add(instrument);

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task Every_seed_instrument_registers_with_a_valid_isin_and_a_unique_symbol()
    {
        var repository = new InMemoryInstruments();
        var register = new RegisterInstrumentHandler(repository);

        foreach (var command in InstrumentSeeder.Load())
        {
            var result = await register.Handle(command, TestContext.Current.CancellationToken);
            (result.IsSuccess ? null : result.Error.Message).ShouldBeNull(command.ProviderSymbols[0]);
        }

        repository.Items.Count.ShouldBe(48);
        repository.Items.Where(i => i.Kind == InstrumentKind.Etf).ShouldAllBe(i => i.Isin != null && i.Group != null);
        repository.Items.ShouldContain(i => i.SymbolFor(QuoteProvider.YFinance)!.Symbol == "^VIX" && i.Kind == InstrumentKind.Index);
    }
}
