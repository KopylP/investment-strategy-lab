using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Strategies.Definitions.Application.ArchiveStrategy;
using InvestmentLab.Strategies.Definitions.Application.CreateDefaultStrategies;
using InvestmentLab.Strategies.Definitions.Application.CreateStrategy;
using InvestmentLab.Strategies.Definitions.Application.GetStrategy;
using InvestmentLab.Strategies.Definitions.Application.ListStrategies;
using InvestmentLab.Strategies.Definitions.Application.ReconfigureStrategy;
using InvestmentLab.Strategies.Definitions.Application.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Strategies.Definitions.Api;

internal static class StrategyEndpoints
{
    public static RouteGroupBuilder MapDefinitions(this RouteGroupBuilder group)
    {
        var strategies = group
            .MapGroup(string.Empty)
            .WithTags("Strategies");

        strategies
            .MapGet(string.Empty, ListStrategies)
            .WithName("ListStrategies");
        strategies
            .MapPost(string.Empty, CreateStrategy)
            .WithName("CreateStrategy")
            .Produces<StrategyDto>();
        strategies
            .MapPost("/defaults", CreateDefaultStrategies)
            .WithName("CreateDefaultStrategies")
            .Produces<IReadOnlyList<StrategyDto>>();
        strategies
            .MapGet("/{strategyId:guid}", GetStrategy)
            .WithName("GetStrategy")
            .Produces<StrategyDto>();
        strategies
            .MapPut("/{strategyId:guid}", ReconfigureStrategy)
            .WithName("ReconfigureStrategy")
            .Produces<StrategyDto>();
        strategies
            .MapPost("/{strategyId:guid}/archive", ArchiveStrategy)
            .WithName("ArchiveStrategy");

        return group;
    }

    private static async Task<Ok<IReadOnlyList<StrategyDto>>> ListStrategies(
        ListStrategiesHandler handler, CancellationToken cancellationToken)
    {
        var strategies = await handler.Handle(cancellationToken);

        return TypedResults.Ok(strategies);
    }

    private static async Task<IResult> CreateStrategy(
        CreateStrategyCommand body, CreateStrategyHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var created = await handler.Handle(user.RequiredId, body, cancellationToken);

        return created.ToHttpResult();
    }

    private static async Task<IResult> CreateDefaultStrategies(
        CreateDefaultStrategiesHandler handler, ICurrentUser user, CancellationToken cancellationToken)
    {
        var created = await handler.Handle(user.RequiredId, cancellationToken);

        return created.ToHttpResult();
    }

    private static async Task<IResult> GetStrategy(
        Guid strategyId, int? version, GetStrategyHandler handler, CancellationToken cancellationToken)
    {
        var strategy = await handler.Handle(strategyId, version, cancellationToken);

        return strategy.ToHttpResult();
    }

    private static async Task<IResult> ReconfigureStrategy(
        Guid strategyId, StrategySetupDraft body, ReconfigureStrategyHandler handler, CancellationToken cancellationToken)
    {
        var reconfigured = await handler.Handle(strategyId, body, cancellationToken);

        return reconfigured.ToHttpResult();
    }

    private static async Task<IResult> ArchiveStrategy(Guid strategyId, ArchiveStrategyHandler handler, CancellationToken cancellationToken)
    {
        var archived = await handler.Handle(strategyId, cancellationToken);

        return archived.ToHttpResult();
    }
}
