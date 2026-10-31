using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application.GetTaxProgress;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYear;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYears;
using InvestmentLab.Taxation.Assessments.Application.ListLossCarryforwards;
using InvestmentLab.Taxation.Assessments.Application.RecordLossCarryforward;
using InvestmentLab.Taxation.Assessments.Application.RefreshTaxAssessments;
using InvestmentLab.Taxation.Assessments.Application.RemoveLossCarryforward;
using InvestmentLab.Taxation.Assessments.Application.Shared;
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

        taxes.MapGet("/years", RefreshAndGetTaxYears).WithName("GetTaxYears");
        taxes.MapGet("/years/{year:int}", RefreshAndGetTaxYear).WithName("GetTaxYear");
        taxes.MapGet("/progress/{year:int}", RefreshAndGetTaxProgress).WithName("GetTaxProgress");
        taxes.MapGet("/losses", GetLossCarryforwards).WithName("GetLossCarryforwards");
        taxes.MapPost("/losses", RecordLossCarryforward).WithName("RecordLossCarryforward");
        taxes.MapDelete("/losses/{lossId:guid}", RemoveLossCarryforward).WithName("RemoveLossCarryforward");

        return group;
    }

    private static async Task<Ok<TaxOverviewDto>> RefreshAndGetTaxYears(
        RefreshTaxAssessmentsHandler refresh, GetTaxYearsHandler query, CancellationToken cancellationToken)
    {
        var refreshed = await refresh.Handle(cancellationToken);
        var overview = await query.Handle(refreshed, cancellationToken);

        return TypedResults.Ok(overview);
    }

    private static async Task<IResult> RefreshAndGetTaxYear(
        int year, RefreshTaxAssessmentsHandler refresh, GetTaxYearHandler query, CancellationToken cancellationToken)
    {
        var refreshed = await refresh.Handle(cancellationToken);
        var taxYear = await query.Handle(year, refreshed, cancellationToken);

        return taxYear.ToHttpResult();
    }

    private static async Task<Ok<TaxProgressDto>> RefreshAndGetTaxProgress(
        int year, RefreshTaxAssessmentsHandler refresh, GetTaxProgressHandler query, CancellationToken cancellationToken)
    {
        var refreshed = await refresh.Handle(cancellationToken);
        var progress = query.Handle(year, refreshed);

        return TypedResults.Ok(progress);
    }

    private static async Task<Ok<IReadOnlyList<LossCarryforwardDto>>> GetLossCarryforwards(
        ListLossCarryforwardsHandler handler, CancellationToken cancellationToken)
    {
        var losses = await handler.Handle(cancellationToken);

        return TypedResults.Ok(losses);
    }

    private static async Task<IResult> RecordLossCarryforward(
        RecordLossCommand body, RecordLossCarryforwardHandler handler, CancellationToken cancellationToken)
    {
        var recorded = await handler.Handle(body, cancellationToken);

        return recorded.ToHttpResult();
    }

    private static async Task<IResult> RemoveLossCarryforward(Guid lossId, RemoveLossCarryforwardHandler handler, CancellationToken cancellationToken)
    {
        var removed = await handler.Handle(lossId, cancellationToken);

        return removed.ToHttpResult();
    }
}
