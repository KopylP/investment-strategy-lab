namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public interface ITrialBacktests
{
    Task<TrialSummary> SummaryOfAsync(Guid strategyId, int version, CancellationToken cancellationToken);
}
