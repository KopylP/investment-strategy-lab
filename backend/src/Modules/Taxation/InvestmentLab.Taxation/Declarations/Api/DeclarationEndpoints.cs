using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Declarations.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Declarations.Api;

internal static class DeclarationEndpoints
{
    public static RouteGroupBuilder MapDeclarations(this RouteGroupBuilder group)
    {
        var declarations = group.MapGroup("/declarations/{year:int}").WithTags("Taxes");
        declarations.MapGet("", async (int year, DeclarationQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Available(year, ct)))
            .WithName("GetDeclarationForms");
        declarations.MapGet("/{code}", async (int year, string code, DeclarationQueries queries, CancellationToken ct) => (await queries.Fill(year, code, ct)).ToHttpResult())
            .WithName("GetDeclaration");
        return group;
    }
}
