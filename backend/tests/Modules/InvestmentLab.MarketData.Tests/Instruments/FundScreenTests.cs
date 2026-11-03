using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Instruments.Infrastructure;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class FundScreenTests
{
    private static readonly FundScreenCriteriaDto Criteria = new(null, "us", null, null, null, []);

    [Fact]
    public async Task A_registered_fund_is_marked_with_its_instrument()
    {
        var instruments = new InMemoryInstruments();
        var instrument = Instrument.Register(InstrumentKind.Etf, Isin.Of("IE00B5BMR087"), "Core", "XETR", Currency.EUR, null, null).Value;
        instrument.AssignProviderSymbol(ProviderSymbol.Of(QuoteProvider.Primary, "AAA"));
        instruments.Add(instrument);
        var screen = new FundScreen(new FixedScreener([Match("AAA"), Match("BBB")]), instruments);

        var result = await screen.ScreenAsync(Criteria, CancellationToken.None);

        result.Funds[0].RegisteredInstrumentId.ShouldBe(instrument.Id.Value);
        result.Funds[1].RegisteredInstrumentId.ShouldBeNull();
        result.ErrorCode.ShouldBeNull();
    }

    [Fact]
    public async Task An_engine_failure_is_reported_as_unavailable()
    {
        var screen = new FundScreen(new FixedScreener(null), new InMemoryInstruments());

        var result = await screen.ScreenAsync(Criteria, CancellationToken.None);

        result.ErrorCode.ShouldBe("market_data.fund_screen.unavailable");
        result.Funds.ShouldBeEmpty();
    }

    [Fact]
    public void Options_are_labelled_with_message_codes()
    {
        var screen = new FundScreen(new FixedScreener([]), new InMemoryInstruments());

        var options = screen.Options();

        options.Exchanges.Count.ShouldBe(8);
        options.Regions.Count.ShouldBe(7);
        foreach (var choice in options.Exchanges.Concat(options.Regions))
            choice.Label.ShouldStartWith("@market_data.");
    }

    private static FundScreenMatch Match(string symbol) => new(symbol, "Fund", "EUR", null, null, null, null);

    private sealed class FixedScreener(IReadOnlyList<FundScreenMatch>? matches) : IFundScreener
    {
        public FundScreenOptions Options() => YFinanceFundScreener.ScreenOptions;

        public Task<IReadOnlyList<FundScreenMatch>?> ScreenAsync(FundScreenQuery query, CancellationToken cancellationToken) =>
            Task.FromResult(matches);
    }
}
