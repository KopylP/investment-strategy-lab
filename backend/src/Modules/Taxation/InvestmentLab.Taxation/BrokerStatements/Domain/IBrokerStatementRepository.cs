namespace InvestmentLab.Taxation.BrokerStatements.Domain;

public interface IBrokerStatementRepository
{
    Task<IReadOnlyList<BrokerStatement>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(BrokerStatement statement);

    void Remove(BrokerStatement statement);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
