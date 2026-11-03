namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public interface IPublishedLayoutRepository
{
    Task<PublishedLayout?> FindAsync(Guid strategyId, int version, CancellationToken cancellationToken);
    void Add(PublishedLayout layout);
}
