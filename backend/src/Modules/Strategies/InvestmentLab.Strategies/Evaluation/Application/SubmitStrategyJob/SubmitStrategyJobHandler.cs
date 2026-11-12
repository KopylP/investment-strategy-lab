using InvestmentLab.SharedKernel;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.SubmitStrategyJob;

public sealed class SubmitStrategyJobHandler(EvaluationPreparer preparer, IStrategyEngine engine) : IHandler
{
    public async Task<Result<StrategyJobTicket>> Handle(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken)
    {
        var prepared = await preparer.PrepareAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var (strategy, version, context) = prepared.Value;

        var jobId = await engine.SubmitAsync(strategy.TypeCode, task, context, cancellationToken);
        if (jobId.IsFailure)
            return jobId.Error;

        return new StrategyJobTicket(jobId.Value, task, strategy.Id.Value, version.Number, context.CashInstrumentId);
    }
}
