using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application;
using InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Strategies.Evaluation.Infrastructure;

internal sealed class StrategyEvaluation(EvaluateStrategyHandler evaluate) : IStrategyEvaluation
{
    public async Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.Decide(request, cancellationToken));

    public async Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.AssessRisk(request, cancellationToken));

    public async Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await evaluate.Submit(task, request, cancellationToken));

    public async Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await evaluate.PollDecision(ticket, cancellationToken));

    public async Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await evaluate.PollRisk(ticket, cancellationToken));

    private static StrategyJobPoll<T> ToContract<T>(EngineJobPoll<T> poll) where T : class =>
        new(poll.State, poll.Result is { } result ? ToContract(result) : null);

    private static StrategyEvaluationResult<T> ToContract<T>(Result<T> result) where T : class =>
        result.IsSuccess ? new(result.Value) : new(null, result.Error.Code, result.Error.Message);
}
