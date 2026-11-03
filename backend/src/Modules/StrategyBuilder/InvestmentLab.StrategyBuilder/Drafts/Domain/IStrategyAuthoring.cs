namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IStrategyAuthoring
{
    Task<Result<TrialVersion>> WriteTrialVersionAsync(UserId owner, Guid? trialStrategyId, AuthoredStrategy strategy, CancellationToken cancellationToken);

    Task<Result> DeleteTrialAsync(Guid trialStrategyId, CancellationToken cancellationToken);

    Task<Result<PublishedVersion>> PublishAsync(UserId owner, PublishedOrigin? origin, string name, AuthoredStrategy strategy, CancellationToken cancellationToken);

    Task<AuthoredVersion?> GetVersionAsync(Guid strategyId, int version, CancellationToken cancellationToken);

    Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken);
}
