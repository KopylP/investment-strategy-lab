using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Application.GenerateReport;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Planning.Api;

/// <param name="Code">Which of the strategies' reports (the schedule lists them); null — the first rebalance report.</param>
public sealed record GenerateReportRequest(string? Code = null, decimal? Contribution = null, DateOnly? AsOf = null);

public sealed record GeneratedReportResponse(Guid ReportId);

internal static class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanning(this RouteGroupBuilder group)
    {
        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Planning");

        // D14: a report on demand — the same use case as the scheduled job; which reports there are, the strategies say.
        account.MapPost("/reports", async (Guid accountId, GenerateReportRequest? body, GenerateReportHandler handler, CancellationToken ct) =>
                (await handler.Handle(new GenerateReportCommand(accountId, body?.Code, body?.Contribution, body?.AsOf), ReportTrigger.OnDemand, ct))
                .Map(id => new GeneratedReportResponse(id.Value)).ToHttpResult())
            .WithName("GenerateReport");

        account.MapGet("/settings", async (Guid accountId, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Get(accountId, ct)).ToHttpResult())
            .WithName("GetPlanSettings");

        account.MapGet("/report-schedule", async (Guid accountId, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Schedule(accountId, ct)).ToHttpResult())
            .WithName("GetReportSchedule");

        account.MapPut("/settings", async (Guid accountId, ChangePlanSettingsCommand body, PlanSettingsHandler handler, CancellationToken ct) =>
                (await handler.Change(accountId, body, ct)).ToHttpResult())
            .WithName("ChangePlanSettings");

        account.MapGet("/open-actions", async (Guid accountId, IReportQueries queries, CancellationToken ct) =>
                TypedResults.Ok(new { count = await queries.GetOpenActionsCount(accountId, ct) }))
            .WithName("GetOpenActionsCount");

        return group;
    }

    private static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> map) =>
        result.IsSuccess ? map(result.Value) : result.Error;
}
