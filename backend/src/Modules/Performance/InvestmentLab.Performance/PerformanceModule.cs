using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Shared.Infrastructure;
using InvestmentLab.Performance.Valuation.Api;
using InvestmentLab.Performance.Valuation.Application;
using InvestmentLab.Performance.Valuation.Application.Integration;
using InvestmentLab.Performance.Valuation.Infrastructure;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Taxation.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Performance;

public sealed class PerformanceModule : IModule
{
    public const string Schema = PerformanceDbContext.SchemaName;

    public string Name => "Performance";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<PerformanceDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IPerformanceRepository, PerformanceRepository>();
        services.AddScoped<PriceBook>();
        services.AddScoped<SnapshotBuilder>();
        services.AddScoped<DashboardQueries>();
        services.AddScheduledJob<RebuildSnapshotsJob>(RebuildSnapshotsJob.Schedule);
        services.AddIntegrationEventHandler<TransactionRecordedV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<TransactionReversedV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<DailyQuotesIngestedV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<QuoteCorrectedV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<ReportGeneratedV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<ReportSupersededV1, PerformanceEventHandlers>();
        services.AddIntegrationEventHandler<TaxAssessmentUpdatedV1, PerformanceEventHandlers>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/performance").MapDashboard();
}
