using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.DecideStrategy;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.DecideSubPortfolio;

public sealed class DecideSubPortfolioHandler(SubPortfolioEvaluationRequests requests, DecideStrategyHandler decide) : IHandler
{
    public async Task<Result<StrategyDecisionDto>> Handle(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var request = await requests.BuildAsync(strategyId, command, cancellationToken);
        if (request.IsFailure)
            return request.Error;

        return await decide.Handle(request.Value, cancellationToken);
    }
}
