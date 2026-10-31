using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Evaluation.Application.PollStrategyRisk;

public sealed class PollStrategyRiskHandler(IStrategyEngine engine)
{
    public async Task<EngineJobPoll<RiskAssessmentDto>> Handle(StrategyJobTicket ticket, CancellationToken cancellationToken)
    {
        var poll = await engine.PollAsync<RiskAssessmentDto>(ticket.JobId, cancellationToken);

        if (poll.Result is not { IsSuccess: true } result)
            return poll;

        var assessment = result.Value with { StrategyId = ticket.StrategyId, StrategyVersion = ticket.StrategyVersion };

        return poll with { Result = assessment };
    }
}
