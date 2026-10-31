using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.Performance.Contracts;
using InvestmentLab.Performance.Shared.Infrastructure;
using InvestmentLab.Performance.Valuation.Api;
using InvestmentLab.Performance.Valuation.Application;
using InvestmentLab.Performance.Valuation.Application.Integration;
using InvestmentLab.Performance.Valuation.Infrastructure;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
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
        services.AddScoped<IValuationHistory, ValuationHistory>();
        services.AddScheduledJob<RebuildSnapshotsJob>(RebuildSnapshotsJob.Schedule);
        services.AddScoped<SnapshotInvalidation>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, InvalidateOnTransactionRecorded>();
        services.AddIntegrationEventHandler<TransactionReversedV1, InvalidateOnTransactionReversed>();
        services.AddIntegrationEventHandler<DailyQuotesIngestedV1, InvalidateOnQuotesIngested>();
        services.AddIntegrationEventHandler<QuoteCorrectedV1, InvalidateOnQuoteCorrected>();
        services.AddIntegrationEventHandler<ReportGeneratedV1, KeepPlanOfGeneratedReport>();
        services.AddIntegrationEventHandler<ReportSupersededV1, SupersedePlanOfReport>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/performance").MapDashboard();
}
