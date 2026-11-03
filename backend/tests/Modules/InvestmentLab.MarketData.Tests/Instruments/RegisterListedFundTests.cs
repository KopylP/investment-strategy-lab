using InvestmentLab.MarketData.Instruments.Application.RegisterListedFund;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class RegisterListedFundTests
{
    [Fact]
    public async Task A_discovered_fund_can_be_registered_by_its_listing_without_an_isin()
    {
        var instruments = new InMemoryInstruments();
        var handler = new RegisterListedFundHandler(instruments, new YFinanceSymbolConvention());

        var result = await handler.Handle(new("AAA", "Fund", "EUR"), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        var fund = instruments.Items.ShouldHaveSingleItem();
        fund.Kind.ShouldBe(InstrumentKind.Etf);
        fund.Isin.ShouldBeNull();
        fund.Ticker.ShouldBe("AAA");
        fund.Id.Value.ShouldBe(result.Value);
    }

    [Fact]
    public async Task A_known_listing_reuses_its_instrument()
    {
        var instruments = new InMemoryInstruments();
        var handler = new RegisterListedFundHandler(instruments, new YFinanceSymbolConvention());

        var first = await handler.Handle(new("aaa", "Fund", "EUR"), CancellationToken.None);
        var second = await handler.Handle(new("AAA", "Fund", "EUR"), CancellationToken.None);

        first.Value.ShouldBe(second.Value);
        instruments.Items.Count.ShouldBe(1);
        instruments.Saves.ShouldBe(1);
    }

    [Fact]
    public async Task A_discovered_fund_without_a_valid_currency_is_not_registered()
    {
        var instruments = new InMemoryInstruments();
        var handler = new RegisterListedFundHandler(instruments, new YFinanceSymbolConvention());

        var result = await handler.Handle(new("AAA", "Fund", ""), CancellationToken.None);

        result.Error.Code.ShouldBe("currency.invalid");
        instruments.Items.ShouldBeEmpty();
    }
}
