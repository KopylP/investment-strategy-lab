extern alias api;

using System.Collections.Concurrent;
using InvestmentLab.IntegrationTests.MarketData;
using InvestmentLab.MarketData.FxRates.Application;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Application;
using InvestmentLab.MarketData.Quotes.Domain;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace InvestmentLab.IntegrationTests.Strategies;

/// <summary>
/// The API on a throwaway Postgres with synthetic quotes for every symbol, talking to the real Python engine
/// started by the AppHost. Every context sent to the engine is recorded.
/// </summary>
public sealed class StrategiesApiFactory : WebApplicationFactory<api::Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();

    public string? StrategyEngineUrl { get; set; }

    public ConcurrentQueue<string> EngineRequests { get; } = new();

    public async ValueTask InitializeAsync() => await _postgres.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:investmentlab", _postgres.GetConnectionString());
        builder.UseSetting("Scheduling:RunAtStartup", "false");                 // tests run the jobs themselves
        builder.UseSetting("Strategies:StrategyEngineUrl", StrategyEngineUrl ?? throw new InvalidOperationException("Set StrategyEngineUrl first."));
        builder.ConfigureTestServices(services =>
        {
            services.AddSingleton<IQuoteProvider, SyntheticQuoteProvider>();
            services.AddSingleton<IFxRateProvider, FakeNbpProvider>();
            services.AddHttpClient<StrategyEngineClient>().AddHttpMessageHandler(() => new RecordingHandler(EngineRequests));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private sealed class RecordingHandler(ConcurrentQueue<string> requests) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Content is not null)
                requests.Enqueue(await request.Content.ReadAsStringAsync(cancellationToken));
            return await base.SendAsync(request, cancellationToken);
        }
    }
}

/// <summary>
/// Two years of business days for any symbol: a smooth trend whose slope depends on the symbol (some up, some down),
/// a small wobble, and a calm VIX of about 15, so the market regime is favourable.
/// </summary>
public sealed class SyntheticQuoteProvider : IQuoteProvider
{
    public static readonly DateOnly LastDay = LastBusinessDay();

    public QuoteProvider Provider => QuoteProvider.YFinance;

    public Task<FetchedQuotes> FetchAsync(IReadOnlyList<string> symbols, DateOnly from, CancellationToken cancellationToken)
    {
        var days = new List<DateOnly>();
        for (var day = LastDay.AddDays(-760); day <= LastDay; day = day.AddDays(1))
        {
            if (BusinessDays.IsBusinessDay(day) && day >= from)
                days.Add(day);
        }

        var series = symbols.Select(symbol => new FetchedSeries(symbol, [.. days.Select((day, n) =>
        {
            var price = PriceOf(symbol, n);
            return new FetchedQuote(day, price, price);
        })])).ToList();
        return Task.FromResult(new FetchedQuotes(series, []));
    }

    private static decimal PriceOf(string symbol, int n)
    {
        if (symbol == "^VIX")
            return 15m + (decimal)Math.Round(Math.Sin(n / 7.0), 2);

        var seed = symbol.Aggregate(17, (hash, c) => hash * 31 + c) & 0x7fff;
        var annualDrift = symbol == "SPY" ? 0.15 : (seed % 50 - 15) / 100.0;   // -15% .. +34% a year
        var value = 50.0 * Math.Exp(annualDrift * n / 252.0) * (1 + 0.01 * Math.Sin(n / 5.0 + seed));
        return decimal.Round((decimal)value, 4);
    }

    private static DateOnly LastBusinessDay()
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);
        while (!BusinessDays.IsBusinessDay(day))
            day = day.AddDays(-1);
        return day;
    }
}
