using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Instruments.Infrastructure.Seed;
using Microsoft.Extensions.Logging.Abstractions;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class InstrumentSeedTests
{
    [Fact]
    public async Task Seeding_fills_the_exposure_of_an_instrument_registered_before_it_existed()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryInstruments();
        var register = new RegisterInstrumentHandler(repository);
        var seed = InstrumentSeeder.Load().First(command => command.AssetClass is not null);
        var withoutExposure = seed with { AssetClass = null, Region = null, AssetCurrency = null };
        var registered = await register.Handle(withoutExposure, cancellationToken);

        await new InstrumentSeeder(repository, register, NullLogger<InstrumentSeeder>.Instance).SeedAsync(cancellationToken);

        var instrument = await repository.FindAsync(InstrumentId.Of(registered.Value), cancellationToken);
        instrument!.AssetClass.ToString().ShouldBe(seed.AssetClass);
        instrument.Region.ToString().ShouldBe(seed.Region);
    }

    [Fact]
    public void Every_tradable_seed_instrument_says_what_it_is_exposed_to()
    {
        var tradable = InstrumentSeeder.Load().Where(command => command.Kind != nameof(InstrumentKind.Index));

        var unclassified = tradable.Where(command => command.AssetClass is null || command.Region is null);

        unclassified.Select(command => command.ProviderSymbols[0]).ShouldBeEmpty();
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

        repository.Items.Count.ShouldBe(49);
        repository.Items.Where(i => i.Kind == InstrumentKind.Etf).ShouldAllBe(i => i.Isin != null && i.Group != null);
        repository.Items.ShouldContain(i => i.SymbolFor(QuoteProvider.YFinance)!.Symbol == "^VIX" && i.Kind == InstrumentKind.Index);
    }
}
