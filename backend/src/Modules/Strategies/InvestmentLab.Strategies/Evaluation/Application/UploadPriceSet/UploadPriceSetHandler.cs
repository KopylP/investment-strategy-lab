using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.UploadPriceSet;

public sealed class UploadPriceSetHandler(EvaluationPreparer preparer, IStrategyEngine engine)
{
    public async Task<Result<PriceSetTicket>> Handle(UploadPriceSetRequest request, CancellationToken cancellationToken)
    {
        var prepared = await preparer.PreparePriceSetAsync(request, cancellationToken);
        if (prepared.IsFailure)
            return prepared.Error;

        var uploaded = await engine.UploadPriceSetAsync(prepared.Value.TypeCode, prepared.Value.Series, cancellationToken);
        if (uploaded.IsFailure)
            return uploaded.Error;

        return new PriceSetTicket(uploaded.Value);
    }
}
