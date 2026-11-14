using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;
using InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;
using InvestmentLab.StrategyBuilder.Trials.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.StrategyBuilder.Trials.Api;

internal static class TrialEndpoints
{
    public static RouteGroupBuilder MapTrials(this RouteGroupBuilder group)
    {
        group
            .MapPost("/drafts/{draftId:guid}/trials", RunTrial)
            .WithName("RunDraftTrial")
            .Produces<TrialRunDto>();
        group
            .MapGet("/drafts/{draftId:guid}/trials", ListTrials)
            .WithName("ListDraftTrials")
            .Produces<IReadOnlyList<TrialRunDto>>();

        return group;
    }

    private static async Task<IResult> RunTrial(
        Guid draftId,
        RunTrialRequest body,
        RunTrialHandler handler,
        CancellationToken cancellationToken)
    {
        var command = body.CommandFor(draftId);
        var result = await handler.Handle(command, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListTrials(
        Guid draftId,
        ListTrialsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(draftId, cancellationToken);

        return result.ToHttpResult();
    }

}
