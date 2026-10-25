using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Portfolio.Transactions.Application.ImportStatement;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Portfolio.Transactions.Api;

public sealed record UploadStatementRequest(string FileName, string Content);

internal static class StatementImportEndpoints
{
    public static RouteGroupBuilder MapStatementImports(this RouteGroupBuilder group)
    {
        var imports = group.MapGroup("/accounts/{accountId:guid}/statement-imports").WithTags("Statement import");

        imports.MapGet("", async (Guid accountId, StatementImportService service, CancellationToken ct) => TypedResults.Ok(await service.List(accountId, ct)))
            .WithName("ListStatementImports");
        imports.MapPost("", async (Guid accountId, UploadStatementRequest body, StatementImportService service, CancellationToken ct) =>
                (await service.Upload(accountId, body.FileName, body.Content, ct)).ToHttpResult())
            .WithName("UploadStatement");
        imports.MapGet("/{importId:guid}", async (Guid accountId, Guid importId, StatementImportService service, CancellationToken ct) =>
                (await service.Get(accountId, importId, ct)).ToHttpResult())
            .WithName("GetStatementImport");
        imports.MapPut("/{importId:guid}", async (Guid accountId, Guid importId, ConfigureImportCommand body, StatementImportService service, CancellationToken ct) =>
                (await service.Configure(accountId, importId, body, ct)).ToHttpResult())
            .WithName("ConfigureStatementImport");
        imports.MapPost("/{importId:guid}/confirm", async (Guid accountId, Guid importId, StatementImportService service, CancellationToken ct) =>
                (await service.Confirm(accountId, importId, ct)).ToHttpResult())
            .WithName("ConfirmStatementImport");
        imports.MapPost("/{importId:guid}/undo", async (Guid accountId, Guid importId, StatementImportService service, CancellationToken ct) =>
                (await service.Undo(accountId, importId, ct)).ToHttpResult())
            .WithName("UndoStatementImport");

        return group;
    }
}
