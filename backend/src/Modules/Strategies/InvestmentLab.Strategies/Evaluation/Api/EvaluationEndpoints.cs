using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateSubPortfolio;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace InvestmentLab.Strategies.Evaluation.Api;

internal static class EvaluationEndpoints
{
    public static RouteGroupBuilder MapEvaluation(this RouteGroupBuilder group)
    {
        var evaluation = group.MapGroup("/{strategyId:guid}").WithTags("Strategy evaluation");

        evaluation.MapPost("/decide", async (Guid strategyId, EvaluateSubPortfolioCommand body, EvaluateSubPortfolioHandler handler, CancellationToken ct) =>
                (await handler.Decide(strategyId, body, ct)).ToHttpResult())
            .WithName("EvaluateStrategy");

        evaluation.MapPost("/risk", async (Guid strategyId, EvaluateSubPortfolioCommand body, EvaluateSubPortfolioHandler handler, CancellationToken ct) =>
                (await handler.AssessRisk(strategyId, body, ct)).ToHttpResult())
            .WithName("AssessStrategyRisk");

        return group;
    }
}
