using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Rebalancing.Reports.Application.ApplyJournalMatches;
using InvestmentLab.Rebalancing.Reports.Application.CloseReport;
using InvestmentLab.Rebalancing.Reports.Application.GetReport;
using InvestmentLab.Rebalancing.Reports.Application.ListReports;
using InvestmentLab.Rebalancing.Reports.Application.MarkActionExecuted;
using InvestmentLab.Rebalancing.Reports.Application.PreviewJournalMatches;
using InvestmentLab.Rebalancing.Reports.Application.RecordPartialFill;
using InvestmentLab.Rebalancing.Reports.Application.Shared;
using InvestmentLab.Rebalancing.Reports.Application.SkipAction;
using InvestmentLab.Rebalancing.Reports.Application.UndoMark;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Rebalancing.Reports.Api;

internal static class ReportsEndpoints
{
    public static RouteGroupBuilder MapReports(this RouteGroupBuilder group)
    {
        var reports = group
            .MapGroup("/reports")
            .WithTags("Reports");

        reports
            .MapGet("/", ListReports)
            .WithName("ListReports");
        reports
            .MapGet("/{reportId:guid}", GetReport)
            .WithName("GetReport")
            .Produces<ReportDetailsDto>();
        reports
            .MapPost("/{reportId:guid}/close", CloseReport)
            .WithName("CloseReport")
            .Produces(StatusCodes.Status204NoContent);
        reports
            .MapGet("/{reportId:guid}/journal-matches", PreviewJournalMatches)
            .WithName("PreviewJournalMatches")
            .Produces<LoadOperationsPreviewDto>();
        reports
            .MapPost("/{reportId:guid}/journal-matches", ApplyJournalMatches)
            .WithName("ApplyJournalMatches")
            .Produces<int>();

        var action = reports.MapGroup("/{reportId:guid}/actions/{actionId:guid}");

        action
            .MapPost("/fills", MarkActionExecuted)
            .WithName("MarkActionExecuted")
            .Produces(StatusCodes.Status204NoContent);
        action
            .MapPost("/partial-fills", RecordPartialFill)
            .WithName("RecordPartialFill")
            .Produces(StatusCodes.Status204NoContent);
        action
            .MapPost("/skip", SkipAction)
            .WithName("SkipAction")
            .Produces(StatusCodes.Status204NoContent);
        action
            .MapPost("/undo", UndoActionMark)
            .WithName("UndoActionMark")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<ReportSummaryDto>>> ListReports(
        Guid? accountId, ListReportsHandler handler, CancellationToken cancellationToken)
    {
        var reports = await handler.Handle(accountId, cancellationToken);

        return TypedResults.Ok(reports);
    }

    private static async Task<IResult> GetReport(Guid reportId, GetReportHandler handler, CancellationToken cancellationToken)
    {
        var report = await handler.Handle(reportId, cancellationToken);

        return report.ToHttpResult();
    }

    private static async Task<IResult> CloseReport(Guid reportId, CloseReportHandler handler, CancellationToken cancellationToken)
    {
        var closed = await handler.Handle(reportId, cancellationToken);

        return closed.ToHttpResult();
    }

    private static async Task<IResult> PreviewJournalMatches(
        Guid reportId, PreviewJournalMatchesHandler handler, CancellationToken cancellationToken)
    {
        var matches = await handler.Handle(reportId, cancellationToken);

        return matches.ToHttpResult();
    }

    private static async Task<IResult> ApplyJournalMatches(
        Guid reportId, ApplyJournalMatchesHandler handler, CancellationToken cancellationToken)
    {
        var applied = await handler.Handle(reportId, cancellationToken);

        return applied.ToHttpResult();
    }

    private static async Task<IResult> MarkActionExecuted(
        Guid reportId, Guid actionId, MarkActionExecutedCommand body, MarkActionExecutedHandler handler, CancellationToken cancellationToken)
    {
        var marked = await handler.Handle(reportId, actionId, body, cancellationToken);

        return marked.ToHttpResult();
    }

    private static async Task<IResult> RecordPartialFill(
        Guid reportId, Guid actionId, RecordPartialFillCommand body, RecordPartialFillHandler handler, CancellationToken cancellationToken)
    {
        var recorded = await handler.Handle(reportId, actionId, body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> SkipAction(
        Guid reportId, Guid actionId, SkipActionCommand body, SkipActionHandler handler, CancellationToken cancellationToken)
    {
        var skipped = await handler.Handle(reportId, actionId, body, cancellationToken);

        return skipped.ToHttpResult();
    }

    private static async Task<IResult> UndoActionMark(
        Guid reportId, Guid actionId, UndoMarkHandler handler, CancellationToken cancellationToken)
    {
        var undone = await handler.Handle(reportId, actionId, cancellationToken);

        return undone.ToHttpResult();
    }
}
