using InvestmentLab.Taxation.BrokerStatements.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.BrokerStatements.Infrastructure;

internal sealed class BrokerStatementRepository(TaxationDbContext db) : IBrokerStatementRepository
{
    public async Task<IReadOnlyList<BrokerStatement>> ForAccountAsync(Guid accountId, CancellationToken cancellationToken) =>
        await db.BrokerStatements.Where(statement => statement.AccountId == accountId).ToListAsync(cancellationToken);

    public void Add(BrokerStatement statement) => db.BrokerStatements.Add(statement);

    public void Remove(BrokerStatement statement) => db.BrokerStatements.Remove(statement);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
