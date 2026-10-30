using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Instruments.Infrastructure.Seed;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class InstrumentSeedTests
{
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
