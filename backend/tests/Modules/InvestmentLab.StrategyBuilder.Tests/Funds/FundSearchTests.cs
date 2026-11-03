using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.StrategyBuilder.Funds.Domain;
using InvestmentLab.StrategyBuilder.Funds.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.StrategyBuilder.Tests.Funds;

public sealed class FundSearchTests
{
    [Fact]
    public async Task One_failed_registration_does_not_stop_the_rest_of_a_thirty_fund_batch()
    {
        var registry = new FundRegistry();
        using var services = Registrations(registry);
        var search = Search(services);
        var funds = Enumerable.Range(0, 30).Select(index => Fund($"F{index}")).ToList();

        var result = await search.AddAsync(funds, CancellationToken.None);

        result.InstrumentIds.Count.ShouldBe(29);
        result.Failed.ShouldHaveSingleItem().ShouldBe(new FundNotAdded("F1", "@builder.funds.registration_failed"));
        registry.Calls.ShouldBe(30);
    }

    [Fact]
    public async Task An_unexpected_registration_failure_does_not_stop_the_next_fund()
    {
        var registry = new FundRegistry { ThrowForFailure = true };
        using var services = Registrations(registry);
        var search = Search(services);

        var result = await search.AddAsync([Fund("F1"), Fund("F2")], CancellationToken.None);

        result.InstrumentIds.Count.ShouldBe(1);
        result.Failed.ShouldHaveSingleItem().Symbol.ShouldBe("F1");
    }

    [Fact]
    public async Task Missing_currency_is_reported_per_fund_instead_of_guessed()
    {
        var registry = new FundRegistry();
        using var services = Registrations(registry);
        var search = Search(services);

        var result = await search.AddAsync([Fund("A") with { Currency = null }, Fund("B")], CancellationToken.None);

        result.InstrumentIds.Count.ShouldBe(1);
        result.Failed.ShouldHaveSingleItem().Reason.ShouldBe("@builder.funds.invalid");
        registry.Calls.ShouldBe(1);
    }

    [Fact]
    public async Task Cancellation_stops_the_batch()
    {
        using var services = Registrations(new FundRegistry());
        var search = Search(services);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => search.AddAsync([Fund("A")], cancellation.Token));
    }

    [Fact]
    public async Task A_search_failure_is_readable()
    {
        using var services = Registrations(new FundRegistry());
        var search = Search(services);
        var criteria = FundCriteria.TryOf("place", null, null, null, null, []).Value;

        var result = await search.SearchAsync(criteria, CancellationToken.None);

        result.Error.Code.ShouldBe("market_data.fund_screen.unavailable");
    }

    private static ServiceProvider Registrations(FundRegistry registry) =>
        new ServiceCollection().AddSingleton<IInstrumentRegistry>(registry).BuildServiceProvider();

    private static FundSearch Search(ServiceProvider services) =>
        new(new UnavailableScreen(), services.GetRequiredService<IServiceScopeFactory>(), NullLogger<FundSearch>.Instance);

    private static FoundFund Fund(string symbol) => new(symbol, "Fund", "EUR", null, null, null, null);

    private sealed class UnavailableScreen : IFundScreen
    {
        public FundScreenOptionsDto Options() => new([], []);

        public Task<FundScreenResultDto> ScreenAsync(FundScreenCriteriaDto criteria, CancellationToken cancellationToken) =>
            Task.FromResult(new FundScreenResultDto([], "market_data.fund_screen.unavailable"));
    }

    private sealed class FundRegistry : IInstrumentRegistry
    {
        public int Calls { get; private set; }

        public bool ThrowForFailure { get; init; }

        public Task<RegisteredInstrumentDto> RegisterAsync(NewInstrumentDto instrument, CancellationToken cancellationToken)
        {
            Calls++;
            instrument.Kind.ShouldBe("Etf");

            if (instrument.Symbol != "F1")
                return Task.FromResult(new RegisteredInstrumentDto(Guid.NewGuid(), null));
            if (ThrowForFailure)
                throw new InvalidOperationException("offline");

            return Task.FromResult(new RegisteredInstrumentDto(null, "@builder.funds.registration_failed"));
        }

        public Task<IReadOnlyList<BrokerTickerMatchDto>> MatchAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ListedSymbolDto?>> ResolveAsync(IReadOnlyList<BrokerTickerDto> tickers, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }
}
