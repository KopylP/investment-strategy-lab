using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Taxation.Assessments.Application.GetTaxProgress;
using InvestmentLab.Taxation.Assessments.Application.GetTaxRegime;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYear;
using InvestmentLab.Taxation.Assessments.Application.GetTaxYears;
using InvestmentLab.Taxation.Assessments.Application.ListLossCarryforwards;
using InvestmentLab.Taxation.Assessments.Application.ListTaxPolicies;
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
        var taxes = group
            .MapGroup(string.Empty)
            .WithTags("Taxes");

        taxes
            .MapGet("/policies", ListTaxPolicies)
            .WithName("ListTaxPolicies");
        taxes
            .MapGet("/regime", GetTaxRegime)
            .WithName("GetTaxRegime");
        taxes
            .MapGet("/years", RefreshAndGetTaxYears)
            .WithName("GetTaxYears");
        taxes
            .MapGet("/years/{year:int}", RefreshAndGetTaxYear)
            .WithName("GetTaxYear")
            .Produces<TaxYearDetailsDto>();
        taxes
            .MapGet("/progress/{year:int}", RefreshAndGetTaxProgress)
            .WithName("GetTaxProgress");
        taxes
            .MapGet("/losses", GetLossCarryforwards)
            .WithName("GetLossCarryforwards");
        taxes
            .MapPost("/losses", RecordLossCarryforward)
            .WithName("RecordLossCarryforward")
            .Produces<LossCarryforwardDto>();
        taxes
            .MapDelete("/losses/{lossId:guid}", RemoveLossCarryforward)
            .WithName("RemoveLossCarryforward");

        return group;
    }

    private static Ok<IReadOnlyList<TaxPolicyDto>> ListTaxPolicies(ListTaxPoliciesHandler handler) => TypedResults.Ok(ListTaxPoliciesHandler.Handle());

    private static async Task<Ok<TaxRegimeDto>> GetTaxRegime(GetTaxRegimeHandler handler, CancellationToken cancellationToken) =>
        TypedResults.Ok(await handler.Handle(cancellationToken));

    private static async Task<Ok<TaxOverviewDto>> RefreshAndGetTaxYears(
        RefreshTaxAssessmentsHandler refresh, GetTaxYearsHandler query, CancellationToken cancellationToken)
    {
        await refresh.Handle(cancellationToken);
        var overview = await query.Handle(cancellationToken);

        return TypedResults.Ok(overview);
    }

    private static async Task<IResult> RefreshAndGetTaxYear(
        int year, RefreshTaxAssessmentsHandler refresh, GetTaxYearHandler query, CancellationToken cancellationToken)
    {
        await refresh.Handle(cancellationToken);
        var taxYear = await query.Handle(year, cancellationToken);

        return taxYear.ToHttpResult();
    }

    private static async Task<Ok<TaxProgressDto>> RefreshAndGetTaxProgress(
        int year, RefreshTaxAssessmentsHandler refresh, GetTaxProgressHandler query, CancellationToken cancellationToken)
    {
        await refresh.Handle(cancellationToken);
        var progress = await query.Handle(year, cancellationToken);

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
