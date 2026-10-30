using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Api;
using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Planning.Infrastructure;
using InvestmentLab.Rebalancing.Reports.Api;
using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Application.Integration;
using InvestmentLab.Rebalancing.Reports.Application.LoadOperations;
using InvestmentLab.Rebalancing.Reports.Application.MarkActions;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reports.Infrastructure;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace InvestmentLab.Rebalancing;

public sealed class RebalancingModule : IModule
{
    public const string Schema = RebalancingDbContext.SchemaName;

    public string Name => "Rebalancing";

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<RebalancingDbContext>(Schema);

        var services = builder.Services;
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<ReportQueriesHandler>();
        services.AddScoped<MarkingHandler>();
        services.AddScoped<LoadOperationsHandler>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, TransactionResults>();
        services.AddIntegrationEventHandler<TransactionRejectedV1, TransactionResults>();

        services.AddScoped<IPlanSettingsRepository, PlanSettingsRepository>();
        services.AddScoped<PlanningInputs>();
        services.AddScoped<SaleTaxes>();
        services.AddScoped<PlanSettingsHandler>();
        services.AddScoped<GenerateMonthlyReportHandler>();
        services.AddScoped<GenerateRiskReviewHandler>();
        services.AddScoped<GenerateReportHandler>();
        services.AddScheduledJob<StrategyReportsJob>(ReportSchedules.Check);

        services.AddScoped<IPlanReviewer, NullPlanReviewer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/rebalancing")
            .MapPlanning()
            .MapReports();
}
