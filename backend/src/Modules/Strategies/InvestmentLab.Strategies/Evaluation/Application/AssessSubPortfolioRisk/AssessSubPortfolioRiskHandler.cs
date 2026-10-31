using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.AssessStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateSubPortfolio;

namespace InvestmentLab.Strategies.Evaluation.Application.AssessSubPortfolioRisk;

public sealed class AssessSubPortfolioRiskHandler(SubPortfolioEvaluationRequests requests, AssessStrategyRiskHandler assessRisk)
{
    public async Task<Result<RiskAssessmentDto>> Handle(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var request = await requests.BuildAsync(strategyId, command, cancellationToken);
        if (request.IsFailure)
            return request.Error;

        return await assessRisk.Handle(request.Value, cancellationToken);
    }
}
