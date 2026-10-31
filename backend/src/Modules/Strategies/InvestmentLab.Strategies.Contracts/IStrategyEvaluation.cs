namespace InvestmentLab.Strategies.Contracts;

public interface IStrategyEvaluation
{
    Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken);

    Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken);

    Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(
        StrategyTask task,
        EvaluateStrategyRequest request,
        CancellationToken cancellationToken);

    Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken);

    Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken);
}
