using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.FxRates.Api;
using InvestmentLab.MarketData.FxRates.Application;
using InvestmentLab.MarketData.FxRates.Application.IngestFxRates;
using InvestmentLab.MarketData.FxRates.Infrastructure;
using InvestmentLab.MarketData.FxRates.Infrastructure.Nbp;
using InvestmentLab.MarketData.Instruments.Api;
using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.AssignProviderSymbol;
using InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;
using InvestmentLab.MarketData.Instruments.Application.GetInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Infrastructure;
using InvestmentLab.MarketData.Instruments.Infrastructure.Seed;
using InvestmentLab.MarketData.Quotes.Api;
using InvestmentLab.MarketData.Quotes.Application;
using InvestmentLab.MarketData.Quotes.Application.CorrectQuote;
using InvestmentLab.MarketData.Quotes.Application.IngestDailyQuotes;
using InvestmentLab.MarketData.Quotes.Infrastructure;
using InvestmentLab.MarketData.Quotes.Infrastructure.YFinance;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.MarketData;

public sealed class MarketDataModule : IModule
{
    public const string Schema = MarketDataDbContext.SchemaName;

    public string Name => "MarketData";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<MarketDataDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IInstrumentRepository, InstrumentRepository>();
        services.AddScoped<RegisterInstrumentHandler>();
        services.AddScoped<AssignProviderSymbolHandler>();
        services.AddScoped<GetInstrumentsHandler>();
        services.AddScoped<DescribeInstrumentsHandler>();
        services.AddScheduledJob<InstrumentDescriptionsJob>(InstrumentDescriptionsJob.Schedule, runAtStartup: true);
        services.AddScoped<IInstrumentCatalog, InstrumentCatalog>();
        services.AddScoped<IInstrumentRegistry, InstrumentRegistry>();
        services.AddScoped<IModuleDataSeeder, InstrumentSeeder>();

        services.AddScoped<IQuoteStore, QuoteStore>();
        services.AddScoped<IQuoteHistory, QuoteHistory>();
        services.AddScoped<IngestDailyQuotesHandler>();
        services.AddScoped<CorrectQuoteHandler>();
        services.AddScheduledJob<NightlyQuotesJob>(NightlyQuotesJob.Schedule, runAtStartup: true);
        AddYFinance(builder);

        services.AddScoped<FxRateStore>();
        services.AddScoped<IFxRateStore>(provider => provider.GetRequiredService<FxRateStore>());
        services.AddScoped<IFxRates>(provider => provider.GetRequiredService<FxRateStore>());
        services.AddScoped<IngestFxRatesHandler>();
        services.AddScheduledJob<DailyFxRatesJob>(DailyFxRatesJob.Schedule, runAtStartup: true);
        services.AddScoped<IFxRateBackfill>(provider => provider.GetRequiredService<IngestFxRatesHandler>());
        services.AddHttpClient<NbpFxRateProvider>(http => http.BaseAddress = new Uri(NbpFxRateProvider.BaseAddress));
        services.AddScoped<IFxRateProvider>(provider => provider.GetRequiredService<NbpFxRateProvider>());
    }

    private static void AddYFinance(IHostApplicationBuilder builder)
    {
        var baseAddress = builder.Configuration["MarketData:StrategyEngineUrl"] ?? "https+http://strategy-engine";

        // Three years of daily quotes for twenty symbols take Yahoo a while: longer timeouts than the defaults.
        // RemoveAllResilienceHandlers is experimental (EXTEXP0001) but the documented way to replace the defaults.
#pragma warning disable EXTEXP0001
        builder.Services.AddHttpClient<YFinanceQuoteProvider>(http => http.BaseAddress = new Uri(baseAddress))
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = TimeSpan.FromMinutes(2);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(4);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(6);
            });
        builder.Services.AddScoped<IQuoteProvider>(services => services.GetRequiredService<YFinanceQuoteProvider>());

        // A statement's unknown tickers, each tried on a few exchanges: a minute is plenty.
        builder.Services.AddHttpClient<YFinanceSymbolLookup>(http => http.BaseAddress = new Uri(baseAddress))
            .RemoveAllResilienceHandlers()
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.AttemptTimeout.Timeout = TimeSpan.FromMinutes(1);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromMinutes(2);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromMinutes(2);
            });
#pragma warning restore EXTEXP0001
        builder.Services.AddScoped<ISymbolLookup>(services => services.GetRequiredService<YFinanceSymbolLookup>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/market-data")
            .MapInstruments()
            .MapQuotes()
            .MapFxRates();
}
