using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public static class EvaluationErrors
{
    public static Error EngineUnavailable(string details) =>
        Error.Conflict(StrategyErrorCodes.EngineUnavailable, $"The strategy engine did not answer: {details}");

    public static Error StrategyFailed(string details) =>
        Error.Conflict("strategy.failed", $"The strategy failed: {details}");

    public static Error EngineRejected(string details) =>
        Error.Validation("strategy.engine_rejected", $"The strategy engine refused the context: {details}");
}
