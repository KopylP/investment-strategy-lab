using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IStrategyRuns
{
    Task<StrategySubmission> SubmitAsync(ReportKind kind, StrategyAsk ask, CancellationToken cancellationToken);

    Task<StrategyPoll> PollAsync(StrategyJob job, CancellationToken cancellationToken);

    StrategyDecision DecisionIn(string answer);

    StrategyRiskAssessment RiskIn(string answer);
}
