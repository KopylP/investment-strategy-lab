using InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;
using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Tests.Instruments;

public sealed class DescribeInstrumentsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 21, 30, 0, TimeSpan.Zero);

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class FakeLookup : ISymbolLookup
    {
        public List<string> Asked { get; } = [];

        public Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken)
        {
            Asked.AddRange(queries.SelectMany(query => query.Candidates));

            return Task.FromResult<IReadOnlyList<SymbolMatch?>>([.. queries.Select(query => query.Candidates[0] switch
            {
                "AVGO" => new SymbolMatch("AVGO", "Broadcom Inc.", "USD", "EQUITY", "Broadcom Inc. designs semiconductors."),
                "XDJE.DE" => new SymbolMatch("XDJE.DE", "Xtrackers MSCI Japan ESG", "EUR", "ETF"),
                _ => (SymbolMatch?)null,
            })]);
        }
    }

    private static Instrument Stock(string name, string? yahooSymbol)
    {
        var instrument = Instrument.Register(InstrumentKind.Stock, null, name, "US", Currency.USD, group: null, isAccumulating: null).Value;

        if (yahooSymbol is not null)
            instrument.AssignProviderSymbol(ProviderSymbol.TryParse($"yfinance:{yahooSymbol}").Value);

        return instrument;
    }

    [Fact]
    public void An_empty_answer_keeps_the_description_there_was()
    {
        var instrument = Stock("Broadcom Inc.", "AVGO");

        instrument.Describe("  Broadcom Inc. designs semiconductors. ", Now);
        instrument.Describe(null, Now.AddDays(31));

        instrument.Description.ShouldBe("Broadcom Inc. designs semiconductors.");
        instrument.DescribedAt.ShouldBe(Now.AddDays(31));
    }

    [Fact]
    public void Yahoo_is_asked_again_only_after_a_month()
    {
        var instrument = Stock("Broadcom Inc.", "AVGO");
        instrument.NeedsDescription(Now).ShouldBeTrue();

        instrument.Describe("Broadcom Inc. designs semiconductors.", Now);

        instrument.NeedsDescription(Now.AddDays(29)).ShouldBeFalse();
        instrument.NeedsDescription(Now.AddDays(30)).ShouldBeTrue();
    }

    [Fact]
    public async Task Instruments_are_described_by_their_yahoo_symbol_and_unknown_ones_are_asked_again_later()
    {
        var repository = new InMemoryInstruments();
        var broadcom = Stock("Broadcom Inc.", "AVGO");
        var japan = Stock("Xtrackers MSCI Japan ESG", "XDJE.DE");
        var unknown = Stock("Gone Inc.", "GONE");
        var noSymbol = Stock("Manual Inc.", null);
        repository.Items.AddRange([broadcom, japan, unknown, noSymbol]);
        var lookup = new FakeLookup();

        var described = await new DescribeInstrumentsHandler(repository, lookup, new FixedClock(Now)).Handle(null, TestContext.Current.CancellationToken);

        described.ShouldBe(2);
        lookup.Asked.ShouldBe(["AVGO", "XDJE.DE", "GONE"]);
        broadcom.Description.ShouldBe("Broadcom Inc. designs semiconductors.");
        japan.Description.ShouldBeNull();
        japan.NeedsDescription(Now).ShouldBeFalse();
        unknown.NeedsDescription(Now).ShouldBeTrue();
        repository.Saves.ShouldBe(1);
    }

    [Fact]
    public async Task Only_the_given_instruments_are_asked_for_even_when_described_recently()
    {
        var repository = new InMemoryInstruments();
        var broadcom = Stock("Broadcom Inc.", "AVGO");
        broadcom.Describe("Old text.", Now.AddDays(-1));
        repository.Items.AddRange([broadcom, Stock("Xtrackers MSCI Japan ESG", "XDJE.DE")]);
        var lookup = new FakeLookup();

        await new DescribeInstrumentsHandler(repository, lookup, new FixedClock(Now)).Handle([broadcom.Id.Value], TestContext.Current.CancellationToken);

        lookup.Asked.ShouldBe(["AVGO"]);
        broadcom.Description.ShouldBe("Broadcom Inc. designs semiconductors.");
    }
}
