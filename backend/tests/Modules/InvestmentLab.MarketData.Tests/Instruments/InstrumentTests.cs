using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class InstrumentTests
{
    private static readonly Isin CoreSp500 = Isin.Of("IE00B5BMR087");

    private static Result<Instrument> RegisterEtf(Isin? isin, string name = "iShares Core S&P 500") =>
        Instrument.Register(InstrumentKind.Etf, isin, name, "XETR", Currency.EUR, InstrumentGroup.Of("Акції США: S&P 500"), isAccumulating: true);

    [Fact]
    public void An_etf_is_registered_with_its_isin()
    {
        var instrument = RegisterEtf(CoreSp500).Value;

        instrument.Isin.ShouldBe(CoreSp500);
        instrument.Kind.ShouldBe(InstrumentKind.Etf);
        instrument.ProviderSymbols.ShouldBeEmpty();
    }

    [Fact]
    public void An_etf_without_isin_is_rejected()
    {
        RegisterEtf(isin: null).Error.Code.ShouldBe("instrument.isin_required");
    }

    [Fact]
    public void An_index_needs_no_isin()
    {
        var vix = Instrument.Register(InstrumentKind.Index, null, "CBOE Volatility Index", "XCBO", Currency.USD, group: null, isAccumulating: null);

        vix.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public void A_blank_name_is_rejected()
    {
        RegisterEtf(CoreSp500, name: "  ").Error.Code.ShouldBe("instrument.name_required");
    }

    [Fact]
    public void Each_provider_keeps_one_symbol_and_a_new_one_replaces_it()
    {
        var instrument = RegisterEtf(CoreSp500).Value;

        instrument.AssignProviderSymbol(ProviderSymbol.Of(QuoteProvider.YFinance, "SXR8.F"));
        instrument.AssignProviderSymbol(ProviderSymbol.Of(QuoteProvider.YFinance, "SXR8.DE"));

        instrument.ProviderSymbols.ShouldHaveSingleItem().Symbol.ShouldBe("SXR8.DE");
        instrument.SymbolFor(QuoteProvider.YFinance)!.ToString().ShouldBe("yfinance:SXR8.DE");
    }

    [Theory]
    [InlineData("yfinance:SXR8.DE", "yfinance", "SXR8.DE")]
    [InlineData(" YFinance:^VIX ", "yfinance", "^VIX")]
    public void A_provider_symbol_is_parsed_from_provider_and_symbol(string text, string provider, string symbol)
    {
        var parsed = ProviderSymbol.TryParse(text).Value;

        parsed.Provider.Code.ShouldBe(provider);
        parsed.Symbol.ShouldBe(symbol);
    }

    [Theory]
    [InlineData("SXR8.DE")]
    [InlineData("yfinance:")]
    [InlineData(":SXR8.DE")]
    public void A_provider_symbol_needs_both_parts(string text)
    {
        ProviderSymbol.TryParse(text).IsFailure.ShouldBeTrue();
    }
}
