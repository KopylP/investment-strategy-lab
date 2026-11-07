using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public interface ITrialBacktests
{
    Task<TrialSummary> SummaryOfAsync(Guid strategyId, int version, CancellationToken cancellationToken);

    Task RequestAsync(TrialVersion version, CancellationToken cancellationToken);
}
