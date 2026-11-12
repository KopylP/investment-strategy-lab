extern alias api;

using InvestmentLab.MarketData.FxRates.Application.Shared;
using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Quotes.Application.Shared;
using System.Collections.Concurrent;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.FxRates.Domain;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InvestmentLab.IntegrationTests.MarketData;

public sealed class MarketDataApiFactory : WebApplicationFactory<api::Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public FakeQuoteProvider Quotes { get; } = new();

    public FakeFundScreener Funds { get; } = new();

    public ConcurrentQueue<QuoteCorrectedV1> CorrectedQuotes { get; } = new();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:investmentlab", _postgres.GetConnectionString());
        builder.UseSetting("Scheduling:RunAtStartup", "false");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveReactionsThatNeedTheStrategyEngine();
            services.AddSingleton<IQuoteProvider>(Quotes);
            services.AddSingleton<IFundScreener>(Funds);
            services.AddSingleton<IFxRateProvider, FakeNbpProvider>();
            services.AddSingleton<ISymbolLookup, FakeSymbolLookup>();
            services.AddSingleton<IStrategyDirectory, NoStrategies>();
            services.AddSingleton(this);
            services.AddIntegrationEventHandler<QuoteCorrectedV1, QuoteCorrectedRecorder>();
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class QuoteCorrectedRecorder(MarketDataApiFactory factory) : IIntegrationEventHandler<QuoteCorrectedV1>
    {
        public Task Handle(QuoteCorrectedV1 integrationEvent, CancellationToken cancellationToken)
        {
            factory.CorrectedQuotes.Enqueue(integrationEvent);

            return Task.CompletedTask;
        }
    }
}

public sealed class NoStrategies : IStrategyDirectory
{
    public Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<StrategySummaryDto>>([]);

    public Task<StrategyReplaySetupDto?> GetReplaySetup(Guid strategyId, int? version, CancellationToken cancellationToken) =>
        Task.FromResult<StrategyReplaySetupDto?>(null);

    public Task<IReadOnlyDictionary<string, string>> GetMessages(string languageCode, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
}

public sealed class FakeQuoteProvider : IQuoteProvider
{
    public const string Symbol = "SXR8.DE";

    public decimal? NewestCloseOverride { get; set; }

    public QuoteProvider Provider => QuoteProvider.YFinance;

    public static IReadOnlyList<DateOnly> TradingDays()
    {
        var days = new List<DateOnly>();

        for (var day = DateOnly.FromDateTime(DateTime.UtcNow); days.Count < 15; day = day.AddDays(-1))
        {
            if (BusinessDays.IsBusinessDay(day))
                days.Insert(0, day);
        }

        return days;
    }

    public Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken)
    {
        var days = TradingDays();
        var quotes = days
            .Select((day, n) => new FetchedQuote(day, day == days[^1] && NewestCloseOverride is { } price ? price : 100m + n, 100m + n))
            .Where(quote => quote.Date >= from)
            .ToList();

        return Task.FromResult(new FetchedQuotes(
            symbols.Contains(Symbol) ? [new FetchedSeries(Symbol, quotes)] : [],
            [.. symbols.Where(symbol => symbol != Symbol)]));
    }
}

public sealed class FakeNbpProvider : IFxRateProvider
{
    public FxRateSource Source => FxRateSource.Nbp;

    public Task<IReadOnlyList<FxRate>> FetchAsync(Currency @base, DateOnly from, DateOnly to, CancellationToken cancellationToken)
    {
        var rates = new List<FxRate>();

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (BusinessDays.IsBusinessDay(day))
                rates.Add(FxRate.Of(FxRateSource.Nbp, @base, Currency.PLN, day, @base == Currency.EUR ? 4.30m : 3.90m, $"{day.DayOfYear:000}/A/NBP/{day.Year}"));
        }

        return Task.FromResult<IReadOnlyList<FxRate>>(rates);
    }
}

public sealed class FakeSymbolLookup : ISymbolLookup
{
    private static readonly SymbolMatch Rheinmetall = new("RHM.DE", "Rheinmetall AG", "EUR", "EQUITY");

    public Task<IReadOnlyList<SymbolMatch?>> LookupAsync(IReadOnlyList<SymbolQuery> queries, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<SymbolMatch?>>([.. queries.Select(query =>
            query.Candidates.Contains(Rheinmetall.Symbol) && query.Currency == Rheinmetall.Currency ? Rheinmetall : null)]);
}
