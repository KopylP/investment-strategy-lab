using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.DecideStrategy;

public sealed class DecideStrategyHandler(EvaluationPreparer preparer, IStrategyEngine engine) : IHandler
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
