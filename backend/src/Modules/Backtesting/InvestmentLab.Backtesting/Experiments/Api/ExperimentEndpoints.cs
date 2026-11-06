using InvestmentLab.Backtesting.Experiments.Application.GetExperiment;
using InvestmentLab.Backtesting.Experiments.Application.GetExperimentJournal;
using InvestmentLab.BuildingBlocks.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Backtesting.Experiments.Api;

internal static class ExperimentEndpoints
{
    public static RouteGroupBuilder MapExperiments(this RouteGroupBuilder group)
    {
        group.MapGet("/experiments/{id:guid}", Get).WithName("GetResearchExperiment");
        group.MapGet("/experiments/{id:guid}/journal", Journal).WithName("GetExperimentJournal");

        return group;
    }

    private static async Task<IResult> Get(Guid id, GetExperimentHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(id, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> Journal(
        Guid id,
        GetExperimentJournalHandler handler,
        CancellationToken cancellationToken,
        int offset = 0,
        int limit = 50,
        string? kind = null,
        DateOnly? from = null,
        DateOnly? to = null)
    {
        var query = new GetExperimentJournalQuery(
            ExperimentId: id,
            Kind: kind,
            From: from,
            To: to,
            Offset: offset,
            Limit: limit);
        var result = await handler.Handle(query, cancellationToken);

        return result.ToHttpResult();
    }
}
