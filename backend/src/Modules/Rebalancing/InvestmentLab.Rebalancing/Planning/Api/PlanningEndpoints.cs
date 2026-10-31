using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.AddSignalReportRule;
using InvestmentLab.Rebalancing.Planning.Application.GetSignalReportRules;
using InvestmentLab.Rebalancing.Planning.Application.RemoveSignalReportRule;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Planning.Api;

public sealed record GenerateReportRequest(string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null);

public sealed record ReportRequestedResponse(Guid RequestId);

internal static class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanning(this RouteGroupBuilder group)
    {
        var rules = group.MapGroup("/signal-report-rules").WithTags("Planning");
        rules.MapGet("", GetSignalReportRules).WithName("GetSignalReportRules");
        rules.MapPost("", AddSignalReportRule).WithName("AddSignalReportRule");
        rules.MapDelete("/{ruleId:guid}", RemoveSignalReportRule).WithName("RemoveSignalReportRule");

        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Planning");

        account.MapPost("/reports", async (Guid accountId, GenerateReportRequest? body, RequestReportHandler handler, CancellationToken ct) =>
            {
                var requested = await handler.Handle(new RequestReportCommand(accountId, body?.Code, body?.Contribution, body?.AsOf), ReportTrigger.OnDemand, ct);

                return requested.IsSuccess
                    ? TypedResults.Accepted($"/api/rebalancing/report-requests/{requested.Value}", new ReportRequestedResponse(requested.Value.Value))
                    : requested.Error.ToProblem();
            })
            .WithName("GenerateReport");

        account.MapGet("/settings", async (Guid accountId, GetPlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, ct)).ToHttpResult())
            .WithName("GetPlanSettings");

        account.MapGet("/report-schedule", async (Guid accountId, GetReportScheduleHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, ct)).ToHttpResult())
            .WithName("GetReportSchedule");

        account.MapPut("/report-schedule/{code}", async (Guid accountId, string code, ChangeReportScheduleCommand body, ChangeReportScheduleHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, code, body, ct)).ToHttpResult())
            .WithName("ChangeReportSchedule");

        account.MapDelete("/report-schedule/{code}", async (Guid accountId, string code, UseStrategyScheduleHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, code, ct)).ToHttpResult())
            .WithName("UseStrategyReportSchedule");

        account.MapPut("/settings", async (Guid accountId, ChangePlanSettingsCommand body, ChangePlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Handle(accountId, body, ct)).ToHttpResult())
            .WithName("ChangePlanSettings");

        account.MapGet("/open-actions", async (Guid accountId, IReportQueries queries, CancellationToken ct) =>
                TypedResults.Ok(new { count = await queries.GetOpenActionsCount(accountId, ct) }))
            .WithName("GetOpenActionsCount");

        var requests = group.MapGroup("/report-requests").WithTags("Planning");

        requests.MapGet("/", async (Guid? accountId, ReportRequestQueriesHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.List(accountId, ct)))
            .WithName("ListReportRequests");

        requests.MapGet("/{requestId:guid}", async (Guid requestId, ReportRequestQueriesHandler handler, CancellationToken ct) =>
                (await handler.Get(requestId, ct)).ToHttpResult())
            .WithName("GetReportRequest");

        return group;
    }

    private static async Task<IResult> GetSignalReportRules(GetSignalReportRulesHandler handler, CancellationToken cancellationToken)
    {
        var rules = await handler.Handle(cancellationToken);

        return rules.ToHttpResult();
    }

    private static async Task<IResult> AddSignalReportRule(
        AddSignalReportRuleCommand body, AddSignalReportRuleHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var added = await handler.Handle(user.RequiredId, body, cancellationToken);

        return added.ToHttpResult();
    }

    private static async Task<IResult> RemoveSignalReportRule(Guid ruleId, RemoveSignalReportRuleHandler handler, CancellationToken cancellationToken)
    {
        var removed = await handler.Handle(ruleId, cancellationToken);

        return removed.ToHttpResult();
    }
}
