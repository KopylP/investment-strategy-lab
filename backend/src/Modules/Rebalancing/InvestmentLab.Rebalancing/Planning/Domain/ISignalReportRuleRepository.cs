namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface ISignalReportRuleRepository
{
    Task<SignalReportRule?> FindAsync(Guid ruleId, CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> AllAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<SignalReportRule>> OfOwnedSignalAsync(Guid signalId, Guid ownerId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid signalId, Guid accountId, string reportCode, CancellationToken cancellationToken);

    void Add(SignalReportRule rule);

    void Remove(SignalReportRule rule);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
