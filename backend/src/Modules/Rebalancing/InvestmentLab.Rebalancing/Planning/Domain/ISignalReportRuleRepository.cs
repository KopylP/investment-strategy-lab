namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface ISignalReportRuleRepository
{
    Task<SignalReportRule?> FindAsync(Guid ruleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> AllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalWithDismissedAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken);

    Task<SignalReportRule?> FindAsync(Guid signalId, Guid accountId, string reportCode, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> AllWithDismissedAsync(CancellationToken cancellationToken);

    void Add(SignalReportRule rule);

    void Remove(SignalReportRule rule);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
