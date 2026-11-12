using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Catalog.Api;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Infrastructure;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Api;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Definitions.Infrastructure;
using InvestmentLab.Strategies.Evaluation.Api;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Strategies;

public sealed class StrategiesModule : IModule
{
    public const string Schema = StrategiesDbContext.SchemaName;

    public string Name => "Strategies";

    public Assembly Assembly => typeof(StrategiesAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<StrategiesDbContext>(Schema);

        var services = builder.Services;
        services.AddMemoryCache();
        AddEngineClient(builder);
        AddCatalog(services);
        AddDefinitions(services);
        AddEvaluation(services);
    }

    private static void AddEngineClient(IHostApplicationBuilder builder)
    {
        var services = builder.Services;
        var engineUrl = builder.Configuration["Strategies:StrategyEngineUrl"] ?? StrategyEngineClient.DefaultBaseAddress;
        services.AddHttpClient<StrategyEngineClient>(http => http.BaseAddress = new Uri(engineUrl));
        services.AddScoped<CatalogSynchronizer>();
    }

    private static void AddCatalog(IServiceCollection services)
    {
        services.AddScoped<IStrategyCatalog, StrategyCatalog>();
    }

    private static void AddDefinitions(IServiceCollection services)
    {
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IStrategyDirectory, StrategyDirectory>();
        services.AddScoped<IStrategyTypes, StrategyTypes>();
        services.AddSingleton<IDefaultStrategies, DefaultStrategies>();
        services.AddScoped<StrategySetupFactory>();
        services.AddScoped<IParameterChecks, EngineParameterChecks>();
        services.AddScoped<IAuthoredStrategies, AuthoredStrategies>();
        services.AddScoped<IStrategyTypeQueries, StrategyTypeQueries>();
    }

    private static void AddEvaluation(IServiceCollection services)
    {
        services.AddScoped<IStrategyEngine, PythonStrategyEngine>();
        services.AddScoped<IStrategyReader, StrategyReader>();
        services.AddScoped<EvaluationPreparer>();
        services.AddScoped<SubPortfolioEvaluationRequests>();
        services.AddScoped<IStrategyEvaluation, StrategyEvaluation>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/strategies")
            .MapCatalog()
            .MapDefinitions()
            .MapEvaluation();
}
