using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;

namespace InvestmentLab.Strategies.Evaluation.Application.DecideStrategy;

public sealed class DecideStrategyHandler(EvaluationPreparer preparer, IStrategyEngine engine)
{
    public async Task<Result<StrategyDecisionDto>> Handle(EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        var prepared = await preparer.PrepareAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (strategy, version, context) = prepared.Value;

        var decision = await engine.DecideAsync(strategy.TypeCode, context, cancellationToken);
        if (decision.IsFailure)
            return decision.Error;

        return decision.Value with
        {
            StrategyId = strategy.Id.Value,
            StrategyVersion = version.Number,
            CashInstrumentId = context.CashInstrumentId,
        };
    }
}
