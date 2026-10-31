using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.AssessStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.DecideStrategy;
using InvestmentLab.Strategies.Evaluation.Application.PollStrategyDecision;
using InvestmentLab.Strategies.Evaluation.Application.PollStrategyRisk;
using InvestmentLab.Strategies.Evaluation.Application.Shared;
using InvestmentLab.Strategies.Evaluation.Application.SubmitStrategyJob;

namespace InvestmentLab.Strategies.Evaluation.Infrastructure;

internal sealed class StrategyEvaluation(
    DecideStrategyHandler decide,
    AssessStrategyRiskHandler assessRisk,
    SubmitStrategyJobHandler submit,
    PollStrategyDecisionHandler pollDecision,
    PollStrategyRiskHandler pollRisk) : IStrategyEvaluation
{
    public async Task<StrategyEvaluationResult<StrategyDecisionDto>> EvaluateAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await decide.Handle(request, cancellationToken));

    public async Task<StrategyEvaluationResult<RiskAssessmentDto>> AssessRiskAsync(EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await assessRisk.Handle(request, cancellationToken));

    public async Task<StrategyEvaluationResult<StrategyJobTicket>> SubmitAsync(StrategyTask task, EvaluateStrategyRequest request, CancellationToken cancellationToken) =>
        ToContract(await submit.Handle(task, request, cancellationToken));

    public async Task<StrategyJobPoll<StrategyDecisionDto>> PollDecisionAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await pollDecision.Handle(ticket, cancellationToken));

    public async Task<StrategyJobPoll<RiskAssessmentDto>> PollRiskAsync(StrategyJobTicket ticket, CancellationToken cancellationToken) =>
        ToContract(await pollRisk.Handle(ticket, cancellationToken));

    private static StrategyJobPoll<T> ToContract<T>(EngineJobPoll<T> poll) where T : class =>
        new(poll.State, poll.Result is { } result ? ToContract(result) : null);

    private static StrategyEvaluationResult<T> ToContract<T>(Result<T> result) where T : class =>
        result.IsSuccess ? new(result.Value) : new(null, result.Error.Code, result.Error.Message);
}
