using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Strategies.Catalog.Api;
using InvestmentLab.Strategies.Catalog.Application;
using InvestmentLab.Strategies.Catalog.Infrastructure;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Api;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Definitions.Application.ArchiveStrategy;
using InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Application.GetStrategy;
using InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;
using InvestmentLab.Strategies.Definitions.Infrastructure;
using InvestmentLab.Strategies.Evaluation.Api;
using InvestmentLab.Strategies.Evaluation.Application;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateSubPortfolio;
using InvestmentLab.Strategies.Evaluation.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Strategies;

public sealed class StrategiesModule : IModule
{
    public const string Schema = StrategiesDbContext.SchemaName;

    public string Name => "Strategies";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<StrategiesDbContext>(Schema);

        var services = builder.Services;
        var engineUrl = builder.Configuration["Strategies:StrategyEngineUrl"] ?? StrategyEngineClient.DefaultBaseAddress;
        services.AddHttpClient<StrategyEngineClient>(http => http.BaseAddress = new Uri(engineUrl));
        services.AddScoped<CatalogSynchronizer>();

        services.AddScoped<IStrategyCatalog, StrategyCatalog>();
        services.AddScoped<GetStrategyCatalogHandler>();

        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IStrategyDirectory, StrategyDirectory>();
        services.AddScoped<IStrategyTypes, StrategyTypes>();
        services.AddSingleton<IDefaultStrategies, DefaultStrategies>();
        services.AddScoped<StrategySetupFactory>();
        services.AddScoped<CreateStrategyHandler>();
        services.AddScoped<ReconfigureStrategyHandler>();
        services.AddScoped<ArchiveStrategyHandler>();
        services.AddScoped<GetStrategyHandler>();
        services.AddScoped<CreateDefaultStrategiesHandler>();
        services.AddIntegrationEventHandler<UserRegisteredV1, DefaultStrategiesForNewUser>();

        services.AddScoped<IStrategyEngine, PythonStrategyEngine>();
        services.AddScoped<IStrategyReader, StrategyReader>();
        services.AddScoped<EvaluateStrategyHandler>();
        services.AddScoped<EvaluateSubPortfolioHandler>();
        services.AddScoped<IStrategyEvaluation, StrategyEvaluation>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/strategies")
            .MapCatalog()
            .MapDefinitions()
            .MapEvaluation();
}
