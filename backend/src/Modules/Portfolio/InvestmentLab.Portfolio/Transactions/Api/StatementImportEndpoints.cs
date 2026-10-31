using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.ImportStatement;
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
        var imports = group.MapGroup("/accounts/{accountId:guid}/statement-imports").WithTags("Statement import");

        imports.MapGet("", ListStatementImports).WithName("ListStatementImports");
        imports.MapPost("", UploadStatement).WithName("UploadStatement");
        imports.MapGet("/{importId:guid}", GetStatementImport).WithName("GetStatementImport");
        imports.MapPut("/{importId:guid}", ConfigureStatementImport).WithName("ConfigureStatementImport");
        imports.MapPost("/{importId:guid}/confirm", ConfirmStatementImport).WithName("ConfirmStatementImport");
        imports.MapPost("/{importId:guid}/undo", UndoStatementImport).WithName("UndoStatementImport");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<StatementImportSummaryDto>>> ListStatementImports(
        Guid accountId, StatementImportService service, CancellationToken cancellationToken)
    {
        var imports = await service.List(accountId, cancellationToken);

        return TypedResults.Ok(imports);
    }

    private static async Task<IResult> UploadStatement(
        Guid accountId, UploadStatementRequest body, StatementImportService service, CancellationToken cancellationToken)
    {
        var uploaded = await service.Upload(accountId, body.FileName, body.Content, cancellationToken);

        return uploaded.ToHttpResult();
    }

    private static async Task<IResult> GetStatementImport(
        Guid accountId, Guid importId, StatementImportService service, CancellationToken cancellationToken)
    {
        var statementImport = await service.Get(accountId, importId, cancellationToken);

        return statementImport.ToHttpResult();
    }

    private static async Task<IResult> ConfigureStatementImport(
        Guid accountId, Guid importId, ConfigureImportCommand body, StatementImportService service, CancellationToken cancellationToken)
    {
        var configured = await service.Configure(accountId, importId, body, cancellationToken);

        return configured.ToHttpResult();
    }

    private static async Task<IResult> ConfirmStatementImport(
        Guid accountId, Guid importId, StatementImportService service, CancellationToken cancellationToken)
    {
        var confirmed = await service.Confirm(accountId, importId, cancellationToken);

        return confirmed.ToHttpResult();
    }

    private static async Task<IResult> UndoStatementImport(
        Guid accountId, Guid importId, StatementImportService service, CancellationToken cancellationToken)
    {
        var undone = await service.Undo(accountId, importId, cancellationToken);

        return undone.ToHttpResult();
    }
}
