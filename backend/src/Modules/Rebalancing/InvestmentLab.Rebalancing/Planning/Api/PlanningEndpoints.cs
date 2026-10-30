using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Planning.Api;

/// <param name="Code">Which of the strategies' reports (the schedule lists them); null — the first rebalance report.</param>
public sealed record GenerateReportRequest(string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null);

/// <summary>The report is being made: follow the request (<c>GET /report-requests/{RequestId}</c>) until it is completed.</summary>
public sealed record ReportRequestedResponse(Guid RequestId);

internal static class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanning(this RouteGroupBuilder group)
    {
        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Planning");

        // D14: a report on demand — the same use case as the scheduled job; which reports there are, the strategies say.
        // Strategies may think for minutes or hours: 202 at once, the report is published when they have answered.
        account.MapPost("/reports", async (Guid accountId, GenerateReportRequest? body, RequestReportHandler handler, CancellationToken ct) =>
            {
                var requested = await handler.Handle(new RequestReportCommand(accountId, body?.Code, body?.Contribution, body?.AsOf), ReportTrigger.OnDemand, ct);
                return requested.IsSuccess
                    ? TypedResults.Accepted($"/api/rebalancing/report-requests/{requested.Value}", new ReportRequestedResponse(requested.Value.Value))
                    : requested.Error.ToProblem();
            })
            .WithName("GenerateReport");

        account.MapGet("/settings", async (Guid accountId, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Get(accountId, ct)).ToHttpResult())
            .WithName("GetPlanSettings");

        account.MapGet("/report-schedule", async (Guid accountId, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Schedule(accountId, ct)).ToHttpResult())
            .WithName("GetReportSchedule");

        account.MapPut("/report-schedule/{code}", async (Guid accountId, string code, ChangeReportScheduleCommand body, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.ChangeSchedule(accountId, code, body, ct)).ToHttpResult())
            .WithName("ChangeReportSchedule");

        account.MapDelete("/report-schedule/{code}", async (Guid accountId, string code, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.UseStrategySchedule(accountId, code, ct)).ToHttpResult())
            .WithName("UseStrategyReportSchedule");

        account.MapPut("/settings", async (Guid accountId, ChangePlanSettingsCommand body, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Change(accountId, body, ct)).ToHttpResult())
            .WithName("ChangePlanSettings");

        account.MapGet("/open-actions", async (Guid accountId, IReportQueries queries, CancellationToken ct) =>
                TypedResults.Ok(new { count = await queries.GetOpenActionsCount(accountId, ct) }))
            .WithName("GetOpenActionsCount");

        var requests = group.MapGroup("/report-requests").WithTags("Planning");

        // Reports being made (and those finished in the last day): «Готується» in the app until the report is there.
        requests.MapGet("/", async (Guid? accountId, ReportRequestQueriesHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.List(accountId, ct)))
            .WithName("ListReportRequests");

        requests.MapGet("/{requestId:guid}", async (Guid requestId, ReportRequestQueriesHandler handler, CancellationToken ct) =>
                (await handler.Get(requestId, ct)).ToHttpResult())
            .WithName("GetReportRequest");

        return group;
    }
}
