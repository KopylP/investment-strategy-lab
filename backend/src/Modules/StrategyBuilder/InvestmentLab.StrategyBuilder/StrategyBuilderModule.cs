using System.Reflection;
using InvestmentLab.StrategyBuilder.Funds.Api;
using InvestmentLab.StrategyBuilder.Funds.Domain;
using InvestmentLab.StrategyBuilder.Funds.Infrastructure;
using InvestmentLab.StrategyBuilder.Trials.Api;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using InvestmentLab.StrategyBuilder.Trials.Domain;
using InvestmentLab.StrategyBuilder.Trials.Infrastructure;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.StrategyBuilder.Drafts.Api;
using InvestmentLab.StrategyBuilder.Drafts.Application.Shared;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;
using InvestmentLab.StrategyBuilder.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.StrategyBuilder;

public sealed class StrategyBuilderModule : IModule
{
    public const string Schema = StrategyBuilderDbContext.SchemaName;

    public string Name => "StrategyBuilder";

    public Assembly Assembly => typeof(StrategyBuilderAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<StrategyBuilderDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IFundSearch, FundSearch>();
        services.AddSingleton<ITrialRunGate, TrialRunGate>();
        services.AddScoped<ITrialBacktests, TrialBacktests>();
        services.AddScoped<ITrialExperiments, TrialExperiments>();
        services.AddScoped<TrialRunSummaries>();
        services.AddScoped<IPublishedLayoutRepository, PublishedLayoutRepository>();
        services.AddScoped<ITrialCleanupRepository, TrialCleanupRepository>();
        services.AddScoped<IOwnSignals, OwnSignals>();
        services.AddScoped<IDraftRepository, DraftRepository>();
        services.AddScoped<IRuleVocabulary, RuleVocabularyFromSchema>();
        services.AddScoped<IInstrumentDirectory, InstrumentDirectory>();
        services.AddScoped<IStrategyAuthoring, StrategyAuthoring>();
        services.AddScoped<DraftDescriber>();
        services.AddScoped<DraftChecker>();
        services.AddScoped<IDraftChecks>(provider => provider.GetRequiredService<DraftChecker>());
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/strategy-builder").MapDrafts().MapTrials().MapFunds();
}
