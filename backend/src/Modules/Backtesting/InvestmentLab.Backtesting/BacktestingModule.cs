using System.Reflection;
using InvestmentLab.Backtesting.Contracts;
using InvestmentLab.Backtesting.Experiments.Api;
using InvestmentLab.Backtesting.Experiments.Domain;
using InvestmentLab.Backtesting.Experiments.Infrastructure;
using InvestmentLab.Backtesting.Replays.Api;
using InvestmentLab.Backtesting.Replays.Application.RunBacktest;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;
using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.Backtesting.Shared.Infrastructure;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Backtesting;

public sealed class BacktestingModule : IModule
{
    public const string Schema = BacktestingDbContext.SchemaName;

    public string Name => "Backtesting";

    public Assembly Assembly => typeof(BacktestingAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<BacktestingDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IBacktestRepository, BacktestRepository>();
        services.AddScoped<IBacktestStatusQueries, BacktestStatusQueries>();
        services.AddScoped<IBacktestRequests, BacktestRequests>();
        services.AddScoped<IReplayedStrategies, ReplayedStrategies>();
        services.AddScoped<IStrategyAnswers, StrategyAnswers>();
        services.AddScoped<ITargetTrades, TargetTrades>();
        services.AddScoped<IPriceTables, PriceTables>();
        services.AddScoped<IReplaySignals, ReplaySignals>();
        services.AddScoped<IReplayTaxes, ReplayTaxes>();
        services.AddSingleton<IReplayComparison, BenchmarkComparison>();
        services.AddSingleton<IReplayComparison, BenchmarkAtEqualRiskComparison>();
        services.AddSingleton<IReplayComparison, UniverseEqualWeightComparison>();
        services.AddSingleton<ReplayComparisonCatalog>();
        services.AddScoped<StrategyReplay>();
        services.AddScoped<IResearchReplay>(provider => provider.GetRequiredService<StrategyReplay>());
        services.AddScoped<IResearchStrategies, ResearchStrategies>();
        services.AddScoped<IExperimentRepository, ExperimentRepository>();
        services.AddScoped<IBacktestExperiments, BacktestExperiments>();
        services.AddSingleton<IReplayRunGate, ReplayRunGate>();
        services.AddOptions<ReplayRunOptions>()
            .BindConfiguration(ReplayRunOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/backtesting").MapBacktests().MapExperiments();
}
