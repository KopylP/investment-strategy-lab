using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Application.GenerateMonthlyReport;
using InvestmentLab.Rebalancing.Planning.Application.GenerateRiskReview;
using InvestmentLab.Rebalancing.Reports.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Planning.Api;

public sealed record GenerateReportRequest(decimal? Contribution = null, DateOnly? AsOf = null);

public sealed record GeneratedReportResponse(Guid ReportId);

internal static class PlanningEndpoints
{
    public static RouteGroupBuilder MapPlanning(this RouteGroupBuilder group)
    {
        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Planning");

        // D14: "Згенерувати рапорт" — the same use case as the scheduled job.
        account.MapPost("/reports", async (Guid accountId, GenerateReportRequest? body, GenerateMonthlyReportHandler handler, CancellationToken ct) =>
                (await handler.Handle(new GenerateMonthlyReportCommand(accountId, body?.Contribution, body?.AsOf), ReportTrigger.OnDemand, ct))
                .Map(id => new GeneratedReportResponse(id.Value)).ToHttpResult())
            .WithName("GenerateReport");

        account.MapPost("/risk-reviews", async (Guid accountId, GenerateReportRequest? body, GenerateRiskReviewHandler handler, CancellationToken ct) =>
                (await handler.Handle(new GenerateRiskReviewCommand(accountId, body?.AsOf), ReportTrigger.OnDemand, ct))
                .Map(id => new GeneratedReportResponse(id.Value)).ToHttpResult())
            .WithName("GenerateRiskReview");

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
