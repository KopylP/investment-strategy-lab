using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Declarations.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Declarations.Api;

internal static class DeclarationEndpoints
{
    public static RouteGroupBuilder MapDeclarations(this RouteGroupBuilder group)
    {
        var declarations = group.MapGroup("/declarations/{year:int}").WithTags("Taxes");

        declarations.MapGet("", GetDeclarationForms).WithName("GetDeclarationForms");
        declarations.MapGet("/{code}", GetDeclaration).WithName("GetDeclaration");
        declarations.MapGet("/{code}/pdf", GetDeclarationPdf).WithName("GetDeclarationPdf");
        declarations.MapPut("/pit-8c/{accountId:guid}", SetPit8C).WithName("SetPit8C");
        declarations.MapDelete("/pit-8c/{accountId:guid}", RemovePit8C).WithName("RemovePit8C");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<DeclarationFormDto>>> GetDeclarationForms(
        int year, DeclarationQueries queries, CancellationToken cancellationToken)
    {
        var forms = await queries.Available(year, cancellationToken);

        return TypedResults.Ok(forms);
    }

    private static async Task<IResult> GetDeclaration(
        int year, string code, DeclarationQueries queries, CancellationToken cancellationToken)
    {
        var declaration = await queries.Fill(year, code, cancellationToken);

        return declaration.ToHttpResult();
    }

    private static async Task<IResult> GetDeclarationPdf(
        int year, string code, string? kind, DeclarationQueries queries, CancellationToken cancellationToken)
    {
        var pdf = await queries.Pdf(year, code, kind ?? "summary", cancellationToken);
        if (pdf.IsFailure)
            return pdf.Error.ToProblem();

        return Results.File(pdf.Value.Content, "application/pdf", pdf.Value.FileName);
    }

    private static async Task<IResult> SetPit8C(
        int year, Guid accountId, Pit8CCommand body, DeclarationQueries queries, CancellationToken cancellationToken)
    {
        var set = await queries.SetPit8C(year, accountId, body, cancellationToken);

        return set.ToHttpResult();
    }

    private static async Task<IResult> RemovePit8C(
        int year, Guid accountId, DeclarationQueries queries, CancellationToken cancellationToken)
    {
        var removed = await queries.RemovePit8C(year, accountId, cancellationToken);

        return removed.ToHttpResult();
    }
}
