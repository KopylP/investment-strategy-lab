using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public interface IStrategyEngine
{
    Task<Result<StrategyDecisionDto>> DecideAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken);

    Task<Result<RiskAssessmentDto>> AssessRiskAsync(string typeCode, StrategyEvaluationContext context, CancellationToken cancellationToken);

    Task<Result<string>> SubmitAsync(string typeCode, StrategyTask task, StrategyEvaluationContext context, CancellationToken cancellationToken);

    Task<EngineJobPoll<T>> PollAsync<T>(string jobId, CancellationToken cancellationToken) where T : class;
}
