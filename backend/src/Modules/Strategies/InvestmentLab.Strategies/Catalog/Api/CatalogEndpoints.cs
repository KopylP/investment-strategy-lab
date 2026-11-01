using InvestmentLab.Strategies.Catalog.Application.GetStrategyCatalog;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Strategies.Catalog.Api;

public sealed record SyncedStrategyCatalogResponse(int StrategyTypes);

internal static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalog(this RouteGroupBuilder group)
    {
        var catalog = group.MapGroup("/catalog").WithTags("Strategy catalog");

        catalog.MapGet("", GetStrategyCatalog).WithName("GetStrategyCatalog");
        catalog.MapGet("/messages", GetStrategyMessages).WithName("GetStrategyMessages");
        catalog.MapPost("/sync", SyncStrategyCatalog).WithName("SyncStrategyCatalog");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<StrategyTypeDto>>> GetStrategyCatalog(
        GetStrategyCatalogHandler handler, CancellationToken cancellationToken)
    {
        var strategyTypes = await handler.Handle(cancellationToken);

        return TypedResults.Ok(strategyTypes);
    }

    private static async Task<Ok<IReadOnlyDictionary<string, string>>> GetStrategyMessages(
        IStrategyDirectory directory, CancellationToken cancellationToken) =>
        TypedResults.Ok(await directory.GetMessages(cancellationToken));

    private static async Task<Ok<SyncedStrategyCatalogResponse>> SyncStrategyCatalog(
        IStrategyCatalog catalog, CancellationToken cancellationToken)
    {
        var strategyTypes = await catalog.SyncAsync(cancellationToken);

        return TypedResults.Ok(new SyncedStrategyCatalogResponse(strategyTypes));
    }
}
