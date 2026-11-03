namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IDraftRepository
{
    Task<StrategyDraft?> FindAsync(Guid id, CancellationToken cancellationToken);

    Task<StrategyDraft?> FindByOriginAsync(Guid strategyId, CancellationToken cancellationToken);

    Task<IReadOnlyList<StrategyDraft>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<StrategyDraft>> ReferencingSignalAsync(Guid signalId, CancellationToken cancellationToken);

    void Add(StrategyDraft draft);

    void Remove(StrategyDraft draft);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
