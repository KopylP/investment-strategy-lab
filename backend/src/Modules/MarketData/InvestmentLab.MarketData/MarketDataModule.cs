using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Api;
using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Application.AssignProviderSymbol;
using InvestmentLab.MarketData.Instruments.Application.GetInstruments;
using InvestmentLab.MarketData.Instruments.Application.RegisterInstrument;
using InvestmentLab.MarketData.Instruments.Infrastructure;
using InvestmentLab.MarketData.Instruments.Infrastructure.Seed;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
        services.AddScoped<IInstrumentCatalog, InstrumentCatalog>();
        services.AddScoped<IModuleDataSeeder, InstrumentSeeder>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/market-data")
            .MapInstruments();
}
