using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Taxation.Assessments.Api;

internal static class TaxEndpoints
{
    public static RouteGroupBuilder MapAssessments(this RouteGroupBuilder group)
    {
        var taxes = group.MapGroup("").WithTags("Taxes");

        taxes.MapGet("/years", GetTaxYears).WithName("GetTaxYears");
        taxes.MapGet("/years/{year:int}", GetTaxYear).WithName("GetTaxYear");
        taxes.MapGet("/progress/{year:int}", GetTaxProgress).WithName("GetTaxProgress");
        taxes.MapGet("/losses", GetLossCarryforwards).WithName("GetLossCarryforwards");
        taxes.MapPost("/losses", RecordLossCarryforward).WithName("RecordLossCarryforward");
        taxes.MapDelete("/losses/{lossId:guid}", RemoveLossCarryforward).WithName("RemoveLossCarryforward");

        return group;
    }

    private static async Task<Ok<TaxOverviewDto>> GetTaxYears(TaxQueries queries, CancellationToken cancellationToken)
    {
        var overview = await queries.Overview(cancellationToken);

        return TypedResults.Ok(overview);
    }

    private static async Task<IResult> GetTaxYear(int year, TaxQueries queries, CancellationToken cancellationToken)
    {
        var taxYear = await queries.Year(year, cancellationToken);

        return taxYear.ToHttpResult();
    }

    private static async Task<Ok<TaxProgressDto>> GetTaxProgress(int year, TaxQueries queries, CancellationToken cancellationToken)
    {
        var progress = await queries.Progress(year, cancellationToken);

        return TypedResults.Ok(progress);
    }

    private static async Task<Ok<IReadOnlyList<LossCarryforwardDto>>> GetLossCarryforwards(
        TaxQueries queries, CancellationToken cancellationToken)
    {
        var losses = await queries.Losses(cancellationToken);

        return TypedResults.Ok(losses);
    }

    private static async Task<IResult> RecordLossCarryforward(
        RecordLossCommand body, TaxQueries queries, CancellationToken cancellationToken)
    {
        var recorded = await queries.RecordLoss(body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> RemoveLossCarryforward(Guid lossId, TaxQueries queries, CancellationToken cancellationToken)
    {
        var removed = await queries.RemoveLoss(lossId, cancellationToken);

        return removed.ToHttpResult();
    }
}
