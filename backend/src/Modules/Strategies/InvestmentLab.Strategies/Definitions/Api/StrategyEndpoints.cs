using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Definitions.Application.ArchiveStrategy;
using InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Application.GetStrategy;
using InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Strategies.Definitions.Api;

internal static class StrategyEndpoints
{
    public static RouteGroupBuilder MapDefinitions(this RouteGroupBuilder group)
    {
        var strategies = group.MapGroup("").WithTags("Strategies");

        strategies.MapGet("", async (GetStrategyHandler handler, CancellationToken ct) => TypedResults.Ok(await handler.List(ct)))
            .WithName("ListStrategies");

        strategies.MapPost("", async (CreateStrategyCommand body, CreateStrategyHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(user.RequiredId, body, ct)).ToHttpResult())
            .WithName("CreateStrategy");

        strategies.MapPost("/defaults", async (CreateDefaultStrategiesHandler handler, ICurrentUser user, CancellationToken ct) =>
                (await handler.Handle(user.RequiredId, ct)).ToHttpResult())
            .WithName("CreateDefaultStrategies");

        strategies.MapGet("/{strategyId:guid}", async (Guid strategyId, int? version, GetStrategyHandler handler, CancellationToken ct) =>
                (await handler.Handle(strategyId, version, ct)).ToHttpResult())
            .WithName("GetStrategy");

        strategies.MapPut("/{strategyId:guid}", async (Guid strategyId, StrategySetupInput body, ReconfigureStrategyHandler handler, CancellationToken ct) =>
                (await handler.Handle(strategyId, body, ct)).ToHttpResult())
            .WithName("ReconfigureStrategy");

        strategies.MapPost("/{strategyId:guid}/archive", async (Guid strategyId, ArchiveStrategyHandler handler, CancellationToken ct) =>
                (await handler.Handle(strategyId, ct)).ToHttpResult())
            .WithName("ArchiveStrategy");

        return group;
    }
}
