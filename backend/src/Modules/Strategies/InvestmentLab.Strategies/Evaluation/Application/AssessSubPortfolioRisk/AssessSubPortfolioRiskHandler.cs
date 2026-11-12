using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.AssessStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.AssessSubPortfolioRisk;

public sealed class AssessSubPortfolioRiskHandler(SubPortfolioEvaluationRequests requests, AssessStrategyRiskHandler assessRisk) : IHandler
{
    public async Task<Result<RiskAssessmentDto>> Handle(Guid strategyId, EvaluateSubPortfolioCommand command, CancellationToken cancellationToken)
    {
        var request = await requests.BuildAsync(strategyId, command, cancellationToken);
        if (request.IsFailure)
            return request.Error;

        return await assessRisk.Handle(request.Value, cancellationToken);
    }
}
