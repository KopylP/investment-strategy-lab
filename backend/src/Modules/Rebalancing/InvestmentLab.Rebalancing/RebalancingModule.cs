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

        builder.Services.AddDomainEventMapper<ReportGenerated, ReportGeneratedV1, ReportGeneratedMapper>();
        builder.Services.AddDomainEventMapper<ReportSuperseded, ReportSupersededV1, ReportSupersededMapper>();
        builder.Services.AddDomainEventMapper<ReportClosed, ReportClosedV1, ReportClosedMapper>();
        builder.Services.AddDomainEventMapper<ContributionMarked, RecordContributionV1, ContributionMarkedMapper>();
        builder.Services.AddDomainEventMapper<TransferMarked, RecordInternalTransferV1, TransferMarkedMapper>();
        builder.Services.AddDomainEventMapper<TradeMarked, RecordTradeV1, TradeMarkedMapper>();
        builder.Services.AddDomainEventMapper<MarkUndone, ReverseTransactionV1, MarkUndoneMapper>();
        builder.Services.AddDomainEventMapper<ReportRequestFailed, ReportRequestFailedV1, ReportRequestFailedMapper>();

        var services = builder.Services;
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

        services.AddScoped<IPlanSettingsRepository, PlanSettingsRepository>();
        services.AddScoped<PlanningInputs>();
        services.AddScoped<SaleTaxes>();
        services.AddScoped<GeneratedReportSaver>();
        services.AddScoped<GetPlanSettingsHandler>();
        services.AddScoped<ChangePlanSettingsHandler>();
        services.AddScoped<GetReportScheduleHandler>();
        services.AddScoped<OwnReportScheduleChange>();
        services.AddScoped<ChangeReportScheduleHandler>();
        services.AddScoped<UseStrategyScheduleHandler>();
        services.AddScoped<IReportScheduleQueries, ReportScheduleQueries>();
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
        services.AddScoped<ISignalReportRuleRepository, SignalReportRuleRepository>();
        services.AddScoped<GetSignalReportRulesHandler>();
        services.AddScoped<AddSignalReportRuleHandler>();
        services.AddScoped<RemoveSignalReportRuleHandler>();
        services.AddIntegrationEventHandler<SignalTriggeredV1, RequestReportsOnSignal>();
        services.AddIntegrationEventHandler<SignalDeletedV1, RemoveRulesOfDeletedSignal>();
        services.AddScheduledJob<StrategyReportsJob>(ReportSchedules.Check);
        services.AddScheduledJob<ReportRequestsJob>(ReportSchedules.Requests);

        services.AddScoped<IPlanReviewer, NullPlanReviewer>();
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints.MapGroup("/api/rebalancing")
            .MapPlanning()
            .MapReports();
}
