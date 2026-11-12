using System.Reflection;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Api;
using InvestmentLab.Rebalancing.Planning.Application.AddSignalReportRule;
using InvestmentLab.Rebalancing.Planning.Application.AdvanceReportRequests;
using InvestmentLab.Rebalancing.Planning.Application.ChangePlanSettings;
using InvestmentLab.Rebalancing.Planning.Application.ChangeReportSchedule;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Planning.Application.GetPlanSettings;
using InvestmentLab.Rebalancing.Planning.Application.GetReportRequest;
using InvestmentLab.Rebalancing.Planning.Application.GetReportSchedule;
using InvestmentLab.Rebalancing.Planning.Application.GetSignalReportRules;
using InvestmentLab.Rebalancing.Planning.Application.Integration;
using InvestmentLab.Rebalancing.Planning.Application.ListReportRequests;
using InvestmentLab.Rebalancing.Planning.Application.RemoveSignalReportRule;
using InvestmentLab.Rebalancing.Planning.Application.EnsureDeclaredSignalRules;
using InvestmentLab.Rebalancing.Planning.Application.RequestDueReports;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.RequestReportOnDemand;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Application.UseStrategySchedule;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Planning.Infrastructure;
using InvestmentLab.Rebalancing.Reports.Api;
using InvestmentLab.Rebalancing.Reports.Application.ApplyJournalMatches;
using InvestmentLab.Rebalancing.Reports.Application.CloseReport;
using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Application.Integration;
using InvestmentLab.Rebalancing.Reports.Application.ListReports;
using InvestmentLab.Rebalancing.Reports.Application.MarkActionExecuted;
using InvestmentLab.Rebalancing.Reports.Application.PreviewJournalMatches;
using InvestmentLab.Rebalancing.Reports.Application.RecordPartialFill;
using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Application.SkipAction;
using InvestmentLab.Rebalancing.Reports.Application.UndoMark;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reports.Infrastructure;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using InvestmentLab.Signals.Contracts;
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
        endpoints.MapGroup("/api/rebalancing")
            .MapPlanning()
            .MapReports();
}
