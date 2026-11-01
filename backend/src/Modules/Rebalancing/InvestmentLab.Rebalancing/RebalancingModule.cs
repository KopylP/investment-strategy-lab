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

    public void AddServices(IHostApplicationBuilder builder)
    {
        builder.AddModuleDbContext<RebalancingDbContext>(Schema);
        AddDomainEventMappers(builder.Services);

        var services = builder.Services;
        AddReports(services);
        AddPlanSettings(services);
        AddReportGeneration(services);
        AddSignalRules(services);
        AddJobs(services);
    }

    private static void AddDomainEventMappers(IServiceCollection services)
    {
        services.AddDomainEventMapper<ReportGenerated, ReportGeneratedV1, ReportGeneratedMapper>();
        services.AddDomainEventMapper<ReportSuperseded, ReportSupersededV1, ReportSupersededMapper>();
        services.AddDomainEventMapper<ReportClosed, ReportClosedV1, ReportClosedMapper>();
        services.AddDomainEventMapper<ContributionMarked, RecordContributionV1, ContributionMarkedMapper>();
        services.AddDomainEventMapper<TransferMarked, RecordInternalTransferV1, TransferMarkedMapper>();
        services.AddDomainEventMapper<TradeMarked, RecordTradeV1, TradeMarkedMapper>();
        services.AddDomainEventMapper<MarkUndone, ReverseTransactionV1, MarkUndoneMapper>();
        services.AddDomainEventMapper<ReportRequestFailed, ReportRequestFailedV1, ReportRequestFailedMapper>();
    }

    private static void AddReports(IServiceCollection services)
    {
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IReportQueries, ReportQueries>();
        services.AddScoped<GetReportHandler>();
        services.AddScoped<ListReportsHandler>();
        services.AddScoped<FillMarker>();
        services.AddScoped<MarkActionExecutedHandler>();
        services.AddScoped<RecordPartialFillHandler>();
        services.AddScoped<SkipActionHandler>();
        services.AddScoped<UndoMarkHandler>();
        services.AddScoped<CloseReportHandler>();
        services.AddScoped<JournalOperationsLoader>();
        services.AddScoped<PreviewJournalMatchesHandler>();
        services.AddScoped<ApplyJournalMatchesHandler>();
        services.AddIntegrationEventHandler<TransactionRecordedV1, LinkRecordedTransaction>();
        services.AddIntegrationEventHandler<TransactionRejectedV1, RollBackRejectedTransaction>();


    }

    private static void AddPlanSettings(IServiceCollection services)
    {
        services.AddScoped<IPlanSettingsRepository, PlanSettingsRepository>();
        services.AddScoped<PlanningInputs>();
        services.AddScoped<SaleTaxes>();
        services.AddScoped<GeneratedReportSaver>();
        services.AddScoped<EnsureDeclaredSignalRulesHandler>();
        services.AddScoped<GetPlanSettingsHandler>();
        services.AddScoped<ChangePlanSettingsHandler>();
        services.AddScoped<GetReportScheduleHandler>();
        services.AddScoped<OwnReportScheduleChange>();
        services.AddScoped<ChangeReportScheduleHandler>();
        services.AddScoped<UseStrategyScheduleHandler>();
        services.AddScoped<IReportScheduleQueries, ReportScheduleQueries>();

    }

    private static void AddReportGeneration(IServiceCollection services)
    {
        services.AddScoped<MonthlyPlanPreparation>();
        services.AddScoped<GenerateMonthlyReportHandler>();
        services.AddScoped<RiskReviewStrategyRequests>();
        services.AddScoped<GenerateRiskReviewHandler>();
        services.AddScoped<IReportRequestRepository, ReportRequestRepository>();
        services.AddScoped<RequestReportHandler>();
        services.AddScoped<RequestReportOnDemandHandler>();
        services.AddScoped<GetReportRequestHandler>();
        services.AddScoped<ListReportRequestsHandler>();
        services.AddScoped<ReportRequestProcessor>();

    }

    private static void AddSignalRules(IServiceCollection services)
    {
        services.AddScoped<ISignalReportRuleRepository, SignalReportRuleRepository>();
        services.AddScoped<GetSignalReportRulesHandler>();
        services.AddScoped<AddSignalReportRuleHandler>();
        services.AddScoped<RemoveSignalReportRuleHandler>();
        services.AddIntegrationEventHandler<SignalTriggeredV1, RequestReportsOnSignal>();
        services.AddIntegrationEventHandler<SignalDeletedV1, RemoveRulesOfDeletedSignal>();

    }

    private static void AddJobs(IServiceCollection services)
    {
        services.AddScheduledJob<StrategyReportsJob>(ReportSchedules.Check);
        services.AddScheduledJob<DeclaredSignalRulesJob>(ReportSchedules.Check);
        services.AddScheduledJob<ReportRequestsJob>(ReportSchedules.Requests);

        services.AddScoped<IPlanReviewer, NullPlanReviewer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/rebalancing")
            .MapPlanning()
            .MapReports();
}
