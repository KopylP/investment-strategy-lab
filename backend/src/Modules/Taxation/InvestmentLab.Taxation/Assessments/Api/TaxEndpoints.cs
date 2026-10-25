using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Assessments.Api;

internal static class TaxEndpoints
{
    public static RouteGroupBuilder MapAssessments(this RouteGroupBuilder group)
    {
        var account = group.MapGroup("/accounts/{accountId:guid}").WithTags("Taxes");

        account.MapGet("/years", async (Guid accountId, TaxQueries queries, CancellationToken ct) => (await queries.Years(accountId, ct)).ToHttpResult())
            .WithName("GetTaxYears");
        account.MapGet("/years/{year:int}", async (Guid accountId, int year, TaxQueries queries, CancellationToken ct) => (await queries.Year(accountId, year, ct)).ToHttpResult())
            .WithName("GetTaxYear");
        account.MapGet("/losses", async (Guid accountId, TaxQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Losses(accountId, ct)))
            .WithName("GetLossCarryforwards");
        account.MapPost("/losses", async (Guid accountId, RecordLossCommand body, TaxQueries queries, CancellationToken ct) => (await queries.RecordLoss(accountId, body, ct)).ToHttpResult())
            .WithName("RecordLossCarryforward");
        account.MapDelete("/losses/{lossId:guid}", async (Guid accountId, Guid lossId, TaxQueries queries, CancellationToken ct) => (await queries.RemoveLoss(accountId, lossId, ct)).ToHttpResult())
            .WithName("RemoveLossCarryforward");

        return group;
    }
}
