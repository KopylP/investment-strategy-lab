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

        reports.MapPost("/{reportId:guid}/close", async (Guid reportId, MarkingHandler handler, CancellationToken ct) =>
                (await handler.Close(reportId, ct)).ToHttpResult())
            .WithName("CloseReport");

        reports.MapGet("/{reportId:guid}/journal-matches", async (Guid reportId, LoadOperationsHandler handler, CancellationToken ct) =>
                (await handler.Preview(reportId, ct)).ToHttpResult())
            .WithName("PreviewJournalMatches");

        reports.MapPost("/{reportId:guid}/journal-matches", async (Guid reportId, LoadOperationsHandler handler, CancellationToken ct) =>
                (await handler.Apply(reportId, ct)).ToHttpResult())
            .WithName("ApplyJournalMatches");

        var action = reports.MapGroup("/{reportId:guid}/actions/{actionId:guid}");

        action.MapPost("/fills", async (Guid reportId, Guid actionId, MarkActionExecutedCommand body, MarkingHandler handler, CancellationToken ct) =>
                (await handler.MarkExecuted(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("MarkActionExecuted");

        action.MapPost("/partial-fills", async (Guid reportId, Guid actionId, RecordPartialFillCommand body, MarkingHandler handler, CancellationToken ct) =>
                (await handler.RecordPartialFill(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("RecordPartialFill");

        action.MapPost("/skip", async (Guid reportId, Guid actionId, SkipActionCommand body, MarkingHandler handler, CancellationToken ct) =>
                (await handler.Skip(reportId, actionId, body, ct)).ToHttpResult())
            .WithName("SkipAction");

        action.MapPost("/undo", async (Guid reportId, Guid actionId, MarkingHandler handler, CancellationToken ct) =>
                (await handler.Undo(reportId, actionId, ct)).ToHttpResult())
            .WithName("UndoActionMark");

        return group;
    }
}
