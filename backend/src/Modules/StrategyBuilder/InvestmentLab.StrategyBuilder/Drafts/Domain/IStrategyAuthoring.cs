namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IStrategyAuthoring
{
    Task<Result<TrialVersion>> WriteTrialVersionAsync(UserId owner, Guid? trialStrategyId, AuthoredStrategy strategy, CancellationToken cancellationToken);

    Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken);
}
