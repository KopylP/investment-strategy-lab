using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.PollStrategyDecision;

public sealed class PollStrategyDecisionHandler(IStrategyEngine engine)
{
    public async Task<EngineJobPoll<StrategyDecisionDto>> Handle(StrategyJobTicket ticket, CancellationToken cancellationToken)
    {
        var poll = await engine.PollAsync<StrategyDecisionDto>(ticket.JobId, cancellationToken);

        if (poll.Result is not { IsSuccess: true } result)
            return poll;

        var decision = result.Value with
        {
            StrategyId = ticket.StrategyId,
            StrategyVersion = ticket.StrategyVersion,
            CashInstrumentId = ticket.CashInstrumentId,
        };

        return poll with { Result = decision };
    }
}
