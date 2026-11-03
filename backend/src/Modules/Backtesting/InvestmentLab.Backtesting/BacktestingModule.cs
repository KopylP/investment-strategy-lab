using InvestmentLab.Backtesting.Replays.Api;
using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;
using InvestmentLab.Backtesting.Replays.Application.Integration;
using InvestmentLab.Backtesting.Replays.Application.RunBacktest;
using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Backtesting.Replays.Domain.Comparisons;
using InvestmentLab.Backtesting.Replays.Infrastructure;
using InvestmentLab.Backtesting.Shared.Infrastructure;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Strategies.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Backtesting;

public sealed class BacktestingModule : IModule
{
    public const string Schema = BacktestingDbContext.SchemaName;

    public string Name => "Backtesting";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<BacktestingDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IBacktestRepository, BacktestRepository>();
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
        services.AddScoped<RunBacktestHandler>();
        services.AddScoped<EnsureBacktestHandler>();
        services.AddScoped<GetBacktestHandler>();
        services.AddScoped<GetMoneyOutcomesHandler>();
        services.AddScheduledJob<BacktestsJob>(BacktestsJob.Schedule);
        services.AddIntegrationEventHandler<StrategyReconfiguredV1, RequestBacktestOnReconfigured>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/backtesting").MapBacktests();
}
