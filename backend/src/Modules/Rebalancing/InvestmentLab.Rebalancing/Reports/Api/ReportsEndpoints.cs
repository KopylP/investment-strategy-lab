using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Application.LoadOperations;
using InvestmentLab.Rebalancing.Reports.Application.MarkActions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Reports.Api;

internal static class ReportsEndpoints
{
    public static RouteGroupBuilder MapReports(this RouteGroupBuilder group)
    {
        var reports = group.MapGroup("/reports").WithTags("Reports");

        reports.MapGet("/", async (Guid? accountId, ReportQueriesHandler handler, CancellationToken ct) =>
                TypedResults.Ok(await handler.List(accountId, ct)))
            .WithName("ListReports");

        reports.MapGet("/{reportId:guid}", async (Guid reportId, ReportQueriesHandler handler, CancellationToken ct) =>
                (await handler.Get(reportId, ct)).ToHttpResult())
            .WithName("GetReport");

        reports.MapPost("/{reportId:guid}/close", async (Guid reportId, CloseReportHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, ct)).ToHttpResult())
            .WithName("CloseReport");

        reports.MapGet("/{reportId:guid}/journal-matches", async (Guid reportId, PreviewJournalMatchesHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, ct)).ToHttpResult())
            .WithName("PreviewJournalMatches");

        reports.MapPost("/{reportId:guid}/journal-matches", async (Guid reportId, ApplyJournalMatchesHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, ct)).ToHttpResult())
            .WithName("ApplyJournalMatches");

        var action = reports.MapGroup("/{reportId:guid}/actions/{actionId:guid}");

        action.MapPost("/fills", async (Guid reportId, Guid actionId, MarkActionExecutedCommand body, MarkActionExecutedHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("MarkActionExecuted");

        action.MapPost("/partial-fills", async (Guid reportId, Guid actionId, RecordPartialFillCommand body, RecordPartialFillHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("RecordPartialFill");

        action.MapPost("/skip", async (Guid reportId, Guid actionId, SkipActionCommand body, SkipActionHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("SkipAction");

        action.MapPost("/undo", async (Guid reportId, Guid actionId, UndoMarkHandler handler, CancellationToken ct) =>
                (await handler.Handle(reportId, actionId, ct)).ToHttpResult())
            .WithName("UndoActionMark");

        return group;
    }
}
