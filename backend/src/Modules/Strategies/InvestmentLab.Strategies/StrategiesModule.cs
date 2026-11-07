using InvestmentLab.Strategies.Definitions.Application.GetAuthoredVersion;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Identity.Contracts;
using InvestmentLab.Signals.Contracts;
using InvestmentLab.Strategies.Catalog.Api;
using InvestmentLab.Strategies.Catalog.Application.GetStrategyCatalog;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Infrastructure;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Api;
using InvestmentLab.Strategies.Definitions.Application.ArchiveStrategy;
using InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Application.CheckParameters;
using InvestmentLab.Strategies.Definitions.Application.DeleteTrialStrategy;
using InvestmentLab.Strategies.Definitions.Application.PublishStrategy;
using InvestmentLab.Strategies.Definitions.Application.WriteTrialVersion;
using InvestmentLab.Strategies.Definitions.Application.GetStrategy;
using InvestmentLab.Strategies.Definitions.Application.Integration;
using InvestmentLab.Strategies.Definitions.Application.ListStrategies;
using InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using InvestmentLab.Strategies.Definitions.Domain;
using InvestmentLab.Strategies.Definitions.Infrastructure;
using InvestmentLab.Strategies.Evaluation.Api;
using InvestmentLab.Strategies.Evaluation.Application.AssessStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.AssessSubPortfolioRisk;
using InvestmentLab.Strategies.Evaluation.Application.DecideStrategy;
using InvestmentLab.Strategies.Evaluation.Application.DecideSubPortfolio;
using InvestmentLab.Strategies.Evaluation.Application.PollStrategyDecision;
using InvestmentLab.Strategies.Evaluation.Application.PollStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Application.SubmitStrategyJob;
using InvestmentLab.Strategies.Evaluation.Application.UploadPriceSet;
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
        builder.Services.AddDomainEventMapper<StrategyReconfigured, StrategyReconfiguredV1, StrategyReconfiguredMapper>();
        builder.Services.AddDomainEventMapper<StrategyDeleted, StrategyDeletedV1, StrategyDeletedMapper>();

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
        services.AddScoped<GetStrategyCatalogHandler>();


    }

    private static void AddDefinitions(IServiceCollection services)
    {
        services.AddIntegrationEventHandler<SignalDeletedV1, MarkStrategiesOfDeletedSignal>();
        services.AddScoped<IStrategyRepository, StrategyRepository>();
        services.AddScoped<IStrategyDirectory, StrategyDirectory>();
        services.AddScoped<IStrategyTypes, StrategyTypes>();
        services.AddSingleton<IDefaultStrategies, DefaultStrategies>();
        services.AddScoped<StrategySetupFactory>();
        services.AddScoped<IParameterChecks, EngineParameterChecks>();
        services.AddScoped<CreateStrategyHandler>();
        services.AddScoped<ReconfigureStrategyHandler>();
        services.AddScoped<ArchiveStrategyHandler>();
        services.AddScoped<DeleteTrialStrategyHandler>();
        services.AddScoped<WriteTrialVersionHandler>();
        services.AddScoped<PublishStrategyHandler>();
        services.AddScoped<CheckParametersHandler>();
        services.AddScoped<IAuthoredStrategies, AuthoredStrategies>();
        services.AddScoped<IStrategyTypeQueries, StrategyTypeQueries>();
        services.AddScoped<GetAuthoredVersionHandler>();
        services.AddScoped<GetStrategyHandler>();
        services.AddScoped<ListStrategiesHandler>();
        services.AddScoped<CreateDefaultStrategiesHandler>();
        services.AddIntegrationEventHandler<UserRegisteredV1, DefaultStrategiesForNewUser>();


    }

    private static void AddEvaluation(IServiceCollection services)
    {
        services.AddScoped<IStrategyEngine, PythonStrategyEngine>();
        services.AddScoped<IStrategyReader, StrategyReader>();
        services.AddScoped<EvaluationPreparer>();
        services.AddScoped<DecideStrategyHandler>();
        services.AddScoped<AssessStrategyRiskHandler>();
        services.AddScoped<UploadPriceSetHandler>();
        services.AddScoped<SubmitStrategyJobHandler>();
        services.AddScoped<PollStrategyDecisionHandler>();
        services.AddScoped<PollStrategyRiskHandler>();
        services.AddScoped<SubPortfolioEvaluationRequests>();
        services.AddScoped<DecideSubPortfolioHandler>();
        services.AddScoped<AssessSubPortfolioRiskHandler>();
        services.AddScoped<IStrategyEvaluation, StrategyEvaluation>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/strategies")
            .MapCatalog()
            .MapDefinitions()
            .MapEvaluation();
}
