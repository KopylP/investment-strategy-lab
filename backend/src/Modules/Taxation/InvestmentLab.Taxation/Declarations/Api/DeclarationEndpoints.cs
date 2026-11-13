using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Declarations.Application.GetDeclaration;
using InvestmentLab.Taxation.Declarations.Application.GetDeclarationPdf;
using InvestmentLab.Taxation.Declarations.Application.ListDeclarationForms;
using InvestmentLab.Taxation.Declarations.Application.RemovePit8C;
using InvestmentLab.Taxation.Declarations.Application.SetPit8C;
using InvestmentLab.Taxation.Declarations.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Declarations.Api;

internal static class DeclarationEndpoints
{
    public static RouteGroupBuilder MapDeclarations(this RouteGroupBuilder group)
    {
        var declarations = group
            .MapGroup("/declarations/{year:int}")
            .WithTags("Taxes");

        declarations
            .MapGet(string.Empty, GetDeclarationForms)
            .WithName("GetDeclarationForms");
        declarations
            .MapGet("/{code}", GetDeclaration)
            .WithName("GetDeclaration")
            .Produces<DeclarationDto>();
        declarations
            .MapGet("/{code}/pdf", GetDeclarationPdf)
            .WithName("GetDeclarationPdf")
            .Produces<Stream>(contentType: "application/pdf");
        declarations
            .MapPut("/pit-8c/{accountId:guid}", SetPit8C)
            .WithName("SetPit8C")
            .Produces(StatusCodes.Status204NoContent);
        declarations
            .MapDelete("/pit-8c/{accountId:guid}", RemovePit8C)
            .WithName("RemovePit8C")
            .Produces(StatusCodes.Status204NoContent);

        return group;
    }

    private static async Task<Ok<IReadOnlyList<DeclarationFormDto>>> GetDeclarationForms(
        int year, ListDeclarationFormsHandler handler, CancellationToken cancellationToken)
    {
        var forms = await handler.Handle(year, cancellationToken);

        return TypedResults.Ok(forms);
    }

    private static async Task<IResult> GetDeclaration(
        int year, string code, GetDeclarationHandler handler, CancellationToken cancellationToken)
    {
        var declaration = await handler.Handle(year, code, cancellationToken);

        return declaration.ToHttpResult();
    }

    private static async Task<IResult> GetDeclarationPdf(
        int year, string code, string? kind, GetDeclarationPdfHandler handler, CancellationToken cancellationToken)
    {
        var pdf = await handler.Handle(year, code, kind ?? "summary", cancellationToken);
        if (pdf.IsFailure)
            return pdf.Error.ToProblem();

        return Results.File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
    }

    private static async Task<IResult> SetPit8C(
        int year, Guid accountId, Pit8CCommand body, SetPit8CHandler handler, CancellationToken cancellationToken)
    {
        var set = await handler.Handle(year, accountId, body, cancellationToken);

        return set.ToHttpResult();
    }

    private static async Task<IResult> RemovePit8C(
        int year, Guid accountId, RemovePit8CHandler handler, CancellationToken cancellationToken)
    {
        var removed = await handler.Handle(year, accountId, cancellationToken);

        return removed.ToHttpResult();
    }
}
