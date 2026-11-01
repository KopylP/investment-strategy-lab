using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class SymbolConventionTests
{
    private static readonly YFinanceSymbolConvention Convention = new();

    [Theory]
    [InlineData("sxr8", "EUR", "SXR8.DE")]
    [InlineData("VOD", "GBP", "VOD.L")]
    [InlineData("NESN", "CHF", "NESN.SW")]
    [InlineData("PKO", "PLN", "PKO.WA")]
    [InlineData("BRK.B", "USD", "BRK-B")]
    [InlineData("AAPL", "USD", "AAPL")]
    public void The_most_likely_symbol_of_a_broker_ticker_comes_first(string ticker, string currency, string expected)
    {
        Convention.CandidatesFor(ticker, currency)[0].ShouldBe(expected);
    }

    [Theory]
    [InlineData("SXR8.DE", "XETRA")]
    [InlineData("VOD.L", "LSE")]
    [InlineData("ABC.XX", "XX")]
    [InlineData("AAPL", "US")]
    public void The_exchange_follows_from_the_symbol(string symbol, string expected)
    {
        Convention.ExchangeOf(symbol).ShouldBe(expected);
    }

    [Theory]
    [InlineData("SXR8.DE", "SXR8", true)]
    [InlineData("SXR8.DE", "SXR", false)]
    [InlineData("SXR8", "SXR8", false)]
    public void A_listing_is_the_ticker_with_an_exchange_suffix(string symbol, string ticker, bool expected)
    {
        Convention.IsListingOf(symbol, ticker).ShouldBe(expected);
    }
}
