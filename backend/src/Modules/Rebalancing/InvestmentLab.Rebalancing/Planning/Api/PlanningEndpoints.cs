using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application.AddSignalReportRule;
using InvestmentLab.Rebalancing.Planning.Application.ChangePlanSettings;
using InvestmentLab.Rebalancing.Planning.Application.ChangeReportSchedule;
using InvestmentLab.Rebalancing.Planning.Application.GetPlanSettings;
using InvestmentLab.Rebalancing.Planning.Application.GetReportRequest;
using InvestmentLab.Rebalancing.Planning.Application.GetReportSchedule;
using InvestmentLab.Rebalancing.Planning.Application.GetSignalReportRules;
using InvestmentLab.Rebalancing.Planning.Application.ListReportRequests;
using InvestmentLab.Rebalancing.Planning.Application.RemoveSignalReportRule;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.RequestReportOnDemand;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Application.UseStrategySchedule;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Planning.Api;

public sealed record GenerateReportRequest(string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null);

public sealed record ReportRequestedResponse(Guid RequestId);

public sealed record OpenActionsCountResponse(int Count);

internal static class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanning(this RouteGroupBuilder group)
    {
        var rules = group.MapGroup("/signal-report-rules").WithTags("Planning");

        rules.MapGet("", GetSignalReportRules).WithName("GetSignalReportRules");
        rules.MapPost("", AddSignalReportRule).WithName("AddSignalReportRule");
        rules.MapDelete("/{ruleId:guid}", RemoveSignalReportRule).WithName("RemoveSignalReportRule");

        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Planning");

        account.MapPost("/reports", GenerateReport).WithName("GenerateReport");
        account.MapGet("/settings", GetPlanSettings).WithName("GetPlanSettings");
        account.MapGet("/report-schedule", GetReportSchedule).WithName("GetReportSchedule");
        account.MapPut("/report-schedule/{code}", ChangeReportSchedule).WithName("ChangeReportSchedule");
        account.MapDelete("/report-schedule/{code}", UseStrategyReportSchedule).WithName("UseStrategyReportSchedule");
        account.MapPut("/settings", ChangePlanSettings).WithName("ChangePlanSettings");
        account.MapGet("/open-actions", GetOpenActionsCount).WithName("GetOpenActionsCount");

        var requests = group.MapGroup("/report-requests").WithTags("Planning");

        requests.MapGet("/", ListReportRequests).WithName("ListReportRequests");
        requests.MapGet("/{requestId:guid}", GetReportRequest).WithName("GetReportRequest");

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

    private static async Task<IResult> RemoveSignalReportRule(
        Guid ruleId, RemoveSignalReportRuleHandler handler, CancellationToken cancellationToken)
    {
        var removed = await handler.Handle(ruleId, cancellationToken);

        return removed.ToHttpResult();
    }

    private static async Task<IResult> GenerateReport(
        Guid accountId, GenerateReportRequest? body, RequestReportOnDemandHandler handler, CancellationToken cancellationToken)
    {
        var command = new RequestReportCommand(accountId, body?.Code, body?.Contribution, body?.AsOf);

        var requested = await handler.Handle(command, cancellationToken);
        if (requested.IsFailure)
            return requested.Error.ToProblem();

        var location = $"/api/rebalancing/report-requests/{requested.Value}";

        return TypedResults.Accepted(location, new ReportRequestedResponse(requested.Value));
    }

    private static async Task<IResult> GetPlanSettings(Guid accountId, GetPlanSettingsHandler handler, CancellationToken cancellationToken)
    {
        var settings = await handler.Handle(accountId, cancellationToken);

        return settings.ToHttpResult();
    }

    private static async Task<IResult> GetReportSchedule(
        Guid accountId, GetReportScheduleHandler handler, CancellationToken cancellationToken)
    {
        var schedule = await handler.Handle(accountId, cancellationToken);

        return schedule.ToHttpResult();
    }

    private static async Task<IResult> ChangeReportSchedule(
        Guid accountId, string code, ChangeReportScheduleCommand body, ChangeReportScheduleHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(accountId, code, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> UseStrategyReportSchedule(
        Guid accountId, string code, UseStrategyScheduleHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(accountId, code, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<IResult> ChangePlanSettings(
        Guid accountId, ChangePlanSettingsCommand body, ChangePlanSettingsHandler handler, CancellationToken cancellationToken)
    {
        var changed = await handler.Handle(accountId, body, cancellationToken);

        return changed.ToHttpResult();
    }

    private static async Task<Ok<OpenActionsCountResponse>> GetOpenActionsCount(
        Guid accountId, IReportQueries queries, CancellationToken cancellationToken)
    {
        var count = await queries.GetOpenActionsCount(accountId, cancellationToken);

        return TypedResults.Ok(new OpenActionsCountResponse(count));
    }

    private static async Task<Ok<IReadOnlyList<ReportRequestDto>>> ListReportRequests(
        Guid? accountId, ListReportRequestsHandler handler, CancellationToken cancellationToken)
    {
        var requests = await handler.Handle(accountId, cancellationToken);

        return TypedResults.Ok(requests);
    }

    private static async Task<IResult> GetReportRequest(
        Guid requestId, GetReportRequestHandler handler, CancellationToken cancellationToken)
    {
        var request = await handler.Handle(requestId, cancellationToken);

        return request.ToHttpResult();
    }
}
