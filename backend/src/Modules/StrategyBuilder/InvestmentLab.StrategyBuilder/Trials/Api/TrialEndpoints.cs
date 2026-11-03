using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.StrategyBuilder.Trials.Application.ListTrials;
using InvestmentLab.StrategyBuilder.Trials.Application.RunTrial;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.StrategyBuilder.Trials.Api;

internal static class TrialEndpoints
{
    public static RouteGroupBuilder MapTrials(this RouteGroupBuilder group)
    {
        group.MapPost("/drafts/{draftId:guid}/trials", RunTrial).WithName("RunDraftTrial");
        group.MapGet("/drafts/{draftId:guid}/trials", ListTrials).WithName("ListDraftTrials");

        return group;
    }

    private static async Task<IResult> RunTrial(Guid draftId, RunTrialRequest body, RunTrialHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(new RunTrialCommand(draftId, body.Label), cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> ListTrials(Guid draftId, ListTrialsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(draftId, cancellationToken);

        return result.ToHttpResult();
    }

}
