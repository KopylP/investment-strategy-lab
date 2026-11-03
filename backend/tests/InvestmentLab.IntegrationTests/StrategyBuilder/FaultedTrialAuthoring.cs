using InvestmentLab.SharedKernel;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.IntegrationTests.StrategyBuilder;

internal sealed class FaultedTrialAuthoring(IStrategyAuthoring inner, TrialDeletionFailure failure) : IStrategyAuthoring
{
    public Task<Result<TrialVersion>> WriteTrialVersionAsync(UserId owner, Guid? trialStrategyId, AuthoredStrategy strategy, CancellationToken cancellationToken) =>
        inner.WriteTrialVersionAsync(owner, trialStrategyId, strategy, cancellationToken);

    public Task<Result> DeleteTrialAsync(Guid trialStrategyId, CancellationToken cancellationToken) => failure.Refuse
        ? Task.FromResult(Result.Failure(Error.Conflict("test.cleanup_refused", "Injected cleanup failure.")))
        : inner.DeleteTrialAsync(trialStrategyId, cancellationToken);

    public Task<Result<PublishedVersion>> PublishAsync(UserId owner, PublishedOrigin? origin, string name, AuthoredStrategy strategy, CancellationToken cancellationToken) =>
        inner.PublishAsync(owner, origin, name, strategy, cancellationToken);

    public Task<AuthoredVersion?> GetVersionAsync(Guid strategyId, int version, CancellationToken cancellationToken) =>
        inner.GetVersionAsync(strategyId, version, cancellationToken);

    public Task<Result<RulesCheck>> CheckAsync(string rules, CancellationToken cancellationToken) => inner.CheckAsync(rules, cancellationToken);
}
