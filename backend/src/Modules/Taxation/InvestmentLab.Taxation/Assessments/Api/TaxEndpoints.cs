using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Assessments.Api;

/// <summary>Tax belongs to the signed-in taxpayer (one PIT-38 over all their accounts), so no account in the routes.</summary>
internal static class TaxEndpoints
{
    public static RouteGroupBuilder MapAssessments(this RouteGroupBuilder group)
    {
        var taxes = group.MapGroup("").WithTags("Taxes");

        taxes.MapGet("/years", async (TaxQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Overview(ct)))
            .WithName("GetTaxYears");
        taxes.MapGet("/years/{year:int}", async (int year, TaxQueries queries, CancellationToken ct) => (await queries.Year(year, ct)).ToHttpResult())
            .WithName("GetTaxYear");
        taxes.MapGet("/years/{year:int}/timeline", async (int year, TaxQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Timeline(year, ct)))
            .WithName("GetTaxTimeline");
        taxes.MapGet("/settings", async (TaxQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Settings(ct)))
            .WithName("GetTaxSettings");
        taxes.MapPut("/settings", async (TaxSettingsDto body, TaxQueries queries, CancellationToken ct) => (await queries.ChangeSettings(body, ct)).ToHttpResult())
            .WithName("ChangeTaxSettings");
        taxes.MapGet("/losses", async (TaxQueries queries, CancellationToken ct) => TypedResults.Ok(await queries.Losses(ct)))
            .WithName("GetLossCarryforwards");
        taxes.MapPost("/losses", async (RecordLossCommand body, TaxQueries queries, CancellationToken ct) => (await queries.RecordLoss(body, ct)).ToHttpResult())
            .WithName("RecordLossCarryforward");
        taxes.MapDelete("/losses/{lossId:guid}", async (Guid lossId, TaxQueries queries, CancellationToken ct) => (await queries.RemoveLoss(lossId, ct)).ToHttpResult())
            .WithName("RemoveLossCarryforward");

        return group;
    }
}
