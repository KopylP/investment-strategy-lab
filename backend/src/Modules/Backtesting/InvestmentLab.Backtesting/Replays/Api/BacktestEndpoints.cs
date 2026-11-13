using InvestmentLab.Backtesting.Replays.Application.EnsureBacktest;
using InvestmentLab.Backtesting.Replays.Application.CompareBacktests;
using InvestmentLab.Backtesting.Replays.Application.GetBacktest;
using InvestmentLab.Backtesting.Replays.Application.GetMoneyOutcomes;
using InvestmentLab.BuildingBlocks.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Backtesting.Replays.Api;

internal static class BacktestEndpoints
{
    public static RouteGroupBuilder MapBacktests(this RouteGroupBuilder group)
    {
        group.MapPost("/comparisons", Compare)
            .WithName("CompareBacktests")
            .Produces<BacktestComparisonDto>();
        group.MapGet("/strategies/{strategyId:guid}", EnsureAndGetBacktest)
            .WithTags("Backtesting")
            .WithName("GetBacktest")
            .Produces<BacktestDto>();

        group.MapGet("/strategies/{strategyId:guid}/money-outcomes", GetMoneyOutcomes)
            .WithTags("Backtesting")
            .WithName("GetMoneyOutcomes")
            .Produces<MoneyOutcomesDto>();

        return group;
    }

    private static async Task<IResult> Compare(CompareBacktestsQuery query, CompareBacktestsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.Handle(query, cancellationToken);

        return result.ToHttpResult();
    }

    private static async Task<IResult> EnsureAndGetBacktest(
        Guid strategyId, int? version, EnsureBacktestHandler ensure, GetBacktestHandler query, CancellationToken cancellationToken)
    {
        var ensured = await ensure.Handle(strategyId, version, cancellationToken);
        if (ensured.IsFailure)
            return Result<BacktestDto>.Failure(ensured.Error).ToHttpResult();

        var backtest = await query.Handle(strategyId, ensured.Value, cancellationToken);

        return backtest.ToHttpResult();
    }

    private static async Task<IResult> GetMoneyOutcomes(
        Guid strategyId, int version, decimal start, decimal contribution, int years, GetMoneyOutcomesHandler query, CancellationToken cancellationToken)
    {
        var outcomes = await query.Handle(new GetMoneyOutcomesQuery(strategyId, version, start, contribution, years), cancellationToken);

        return outcomes.ToHttpResult();
    }
}
