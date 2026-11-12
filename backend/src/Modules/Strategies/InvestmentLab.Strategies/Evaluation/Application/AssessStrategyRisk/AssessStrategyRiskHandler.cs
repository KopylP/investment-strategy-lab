using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.AssessStrategyRisk;

public sealed class AssessStrategyRiskHandler(EvaluationPreparer preparer, IStrategyEngine engine) : IHandler
{
    public async Task<Result<RiskAssessmentDto>> Handle(EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        var prepared = await preparer.PrepareAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (strategy, version, context) = prepared.Value;

        var assessment = await engine.AssessRiskAsync(strategy.TypeCode, context, cancellationToken);
        if (assessment.IsFailure)
            return assessment.Error;

        return assessment.Value with { StrategyId = strategy.Id.Value, StrategyVersion = version.Number };
    }
}
