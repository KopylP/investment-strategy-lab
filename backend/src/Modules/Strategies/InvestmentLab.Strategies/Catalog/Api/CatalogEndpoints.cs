using InvestmentLab.Strategies.Catalog.Application;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Strategies.Catalog.Api;

internal static class CatalogEndpoints
{
    public static RouteGroupBuilder MapCatalog(this RouteGroupBuilder group)
    {
        var catalog = group.MapGroup("/catalog").WithTags("Strategy catalog");

        catalog.MapGet("", async (GetStrategyCatalogHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.Handle(ct)))
            .WithName("GetStrategyCatalog");

        catalog.MapPost("/sync", async (IStrategyCatalog strategyCatalog, CancellationToken ct) =>
                TypedResults.Ok(new { strategyTypes = await strategyCatalog.SyncAsync(ct) }))
            .WithName("SyncStrategyCatalog");

        return group;
    }
}
