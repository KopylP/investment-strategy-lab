using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.ConfigureStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ConfirmStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.GetStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.ListStatementImports;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Application.UndoStatementImport;
using InvestmentLab.Portfolio.Transactions.Application.UploadStatement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

public sealed record UploadStatementRequest(string FileName, string Content);

internal static class StatementImportEndpoints
{
    public static RouteGroupBuilder MapStatementImports(this RouteGroupBuilder group)
    {
        var imports = group
            .MapGroup("/accounts/{accountId:guid}/statement-imports")
            .WithTags("Statement import");

        imports
            .MapGet(string.Empty, ListStatementImports)
            .WithName("ListStatementImports");
        imports
            .MapPost(string.Empty, UploadStatement)
            .WithName("UploadStatement")
            .Produces<StatementImportDto>();
        imports
            .MapGet("/{importId:guid}", GetStatementImport)
            .WithName("GetStatementImport")
            .Produces<StatementImportDto>();
        imports
            .MapPut("/{importId:guid}", ConfigureStatementImport)
            .WithName("ConfigureStatementImport")
            .Produces<StatementImportDto>();
        imports
            .MapPost("/{importId:guid}/confirm", ConfirmStatementImport)
            .WithName("ConfirmStatementImport")
            .Produces<StatementImportDto>();
        imports
            .MapPost("/{importId:guid}/undo", UndoStatementImport)
            .WithName("UndoStatementImport")
            .Produces<StatementImportDto>();

        return group;
    }

    private static async Task<Ok<IReadOnlyList<StatementImportSummaryDto>>> ListStatementImports(
        Guid accountId,
        ListStatementImportsHandler handler,
        CancellationToken cancellationToken)
    {
        var imports = await handler.Handle(accountId, cancellationToken);

        return TypedResults.Ok(imports);
    }

    private static async Task<IResult> UploadStatement(
        Guid accountId,
        UploadStatementRequest body,
        UploadStatementHandler handler,
        CancellationToken cancellationToken)
    {
        var uploaded = await handler.Handle(accountId, body.FileName, body.Content, cancellationToken);

        return uploaded.ToHttpResult();
    }

    private static async Task<IResult> GetStatementImport(
        Guid accountId,
        Guid importId,
        GetStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var statementImport = await handler.Handle(accountId, importId, cancellationToken);

        return statementImport.ToHttpResult();
    }

    private static async Task<IResult> ConfigureStatementImport(
        Guid accountId,
        Guid importId,
        ConfigureImportCommand body,
        ConfigureStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var configured = await handler.Handle(accountId, importId, body, cancellationToken);

        return configured.ToHttpResult();
    }

    private static async Task<IResult> ConfirmStatementImport(
        Guid accountId,
        Guid importId,
        ConfirmStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var confirmed = await handler.Handle(accountId, importId, cancellationToken);

        return confirmed.ToHttpResult();
    }

    private static async Task<IResult> UndoStatementImport(
        Guid accountId,
        Guid importId,
        UndoStatementImportHandler handler,
        CancellationToken cancellationToken)
    {
        var undone = await handler.Handle(accountId, importId, cancellationToken);

        return undone.ToHttpResult();
    }
}
