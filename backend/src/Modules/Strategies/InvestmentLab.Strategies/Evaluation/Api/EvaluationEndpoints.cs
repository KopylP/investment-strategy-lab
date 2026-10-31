using InvestmentLab.BuildingBlocks.Http;
using InvestmentLab.Strategies.Evaluation.Application.AssessSubPortfolioRisk;
using InvestmentLab.Strategies.Evaluation.Application.DecideSubPortfolio;
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

        evaluation.MapPost("/decide", Decide).WithName("EvaluateStrategy");
        evaluation.MapPost("/risk", AssessRisk).WithName("AssessStrategyRisk");

        return group;
    }

    private static async Task<IResult> Decide(
        Guid strategyId, EvaluateSubPortfolioCommand body, DecideSubPortfolioHandler handler, CancellationToken cancellationToken)
    {
        var decision = await handler.Handle(strategyId, body, cancellationToken);

        return decision.ToHttpResult();
    }

    private static async Task<IResult> AssessRisk(
        Guid strategyId, EvaluateSubPortfolioCommand body, AssessSubPortfolioRiskHandler handler, CancellationToken cancellationToken)
    {
        var assessment = await handler.Handle(strategyId, body, cancellationToken);

        return assessment.ToHttpResult();
    }
}
