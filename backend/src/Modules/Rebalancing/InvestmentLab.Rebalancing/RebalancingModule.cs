using System.Reflection;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Api;
using InvestmentLab.Rebalancing.Planning.Application.AdvanceReportRequests;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Planning.Infrastructure;
using InvestmentLab.Rebalancing.Reports.Api;
using InvestmentLab.Rebalancing.Reports.Application.Shared;
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

    public Assembly Assembly => typeof(RebalancingAssemblyMarker).Assembly;

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<RebalancingDbContext>(Schema);

        var services = builder.Services;
        AddReports(services);
        AddPlanSettings(services);
        AddReportGeneration(services);
        AddSignalRules(services);
        AddJobs(services);
    }

    private static void AddReports(IServiceCollection services)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<FillMarker>();
        services.AddScoped<JournalOperationsLoader>();
    }

    private static void AddPlanSettings(IServiceCollection services)
    {
        services.AddScoped<IPlanSettingsRepository, PlanSettingsRepository>();
        services.AddScoped<IPlanningAccounts, PlanningAccounts>();
        services.AddScoped<IAccountReports, AccountReports>();
        services.AddScoped<IAccountHoldings, AccountHoldings>();
        services.AddScoped<IInstrumentPrices, InstrumentPrices>();
        services.AddScoped<IInstrumentLabels, InstrumentLabels>();
        services.AddScoped<IStrategyRuns, StrategyRuns>();
        services.AddScoped<SaleTaxes>();
        services.AddScoped<GeneratedReportSaver>();
        services.AddScoped<OwnReportScheduleChange>();
        services.AddScoped<IReportScheduleQueries, ReportScheduleQueries>();
    }

    private static void AddReportGeneration(IServiceCollection services)
    {
        services.AddScoped<MonthlyPlanPreparation>();
        services.AddScoped<MonthlyReportGenerator>();
        services.AddScoped<RiskReviewStrategyRequests>();
        services.AddScoped<RiskReviewGenerator>();
        services.AddScoped<IReportRequestRepository, ReportRequestRepository>();
        services.AddScoped<ReportRequestProcessor>();
    }

    private static void AddSignalRules(IServiceCollection services)
    {
        services.AddScoped<ISignalReportRuleRepository, SignalReportRuleRepository>();
    }

    private static void AddJobs(IServiceCollection services)
    {
        services.AddScoped<IRebalancingRules, RebalancingRules>();

        services.AddScoped<IPlanReviewer, NullPlanReviewer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGroup("/api/rebalancing")
            .MapPlanning()
            .MapReports();
}
