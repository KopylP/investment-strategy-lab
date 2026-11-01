using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class BrokerTickerMatcherTests
{
    private static readonly YFinanceSymbolConvention Convention = new();

    private static Instrument Listed(string? symbol, Currency currency)
    {
        var instrument = Instrument.Register(InstrumentKind.Stock, null, symbol ?? "Unlisted", "X", currency, group: null, isAccumulating: null).Value;

        if (symbol is not null)
            instrument.AssignProviderSymbol(ProviderSymbol.Of(QuoteProvider.Primary, symbol));

        return instrument;
    }

    [Fact]
    public void The_exact_symbol_in_the_currency_of_the_row_wins()
    {
        var listing = Listed("ABC.DE", Currency.EUR);
        var exact = Listed("ABC", Currency.EUR);

        new BrokerTickerMatcher([listing, exact], Convention).Find("abc", "EUR").ShouldBe(exact);
    }

    [Fact]
    public void A_listing_in_the_currency_beats_an_exact_symbol_in_another_currency()
    {
        var american = Listed("ABC", Currency.USD);
        var german = Listed("ABC.DE", Currency.EUR);

        new BrokerTickerMatcher([american, german], Convention).Find("ABC", "EUR").ShouldBe(german);
    }

    [Fact]
    public void Without_the_currency_any_symbol_of_the_ticker_is_taken()
    {
        var german = Listed("ABC.DE", Currency.EUR);

        new BrokerTickerMatcher([german], Convention).Find("ABC", "USD").ShouldBe(german);
    }

    [Fact]
    public void Unknown_and_unlisted_instruments_do_not_match()
    {
        var other = Listed("ABCD.DE", Currency.EUR);
        var unlisted = Listed(null, Currency.EUR);

        new BrokerTickerMatcher([other, unlisted], Convention).Find("ABC", "EUR").ShouldBeNull();
    }

    [Fact]
    public void The_ticker_of_an_instrument_is_its_symbol_at_the_primary_provider()
    {
        Listed("ABC.DE", Currency.EUR).Ticker.ShouldBe("ABC.DE");
        Listed(null, Currency.EUR).Ticker.ShouldBeNull();
    }
}
