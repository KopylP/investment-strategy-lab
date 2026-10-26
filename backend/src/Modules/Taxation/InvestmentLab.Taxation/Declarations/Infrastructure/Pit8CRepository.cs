using InvestmentLab.Taxation.Declarations.Application;
using InvestmentLab.Taxation.Declarations.Domain;
using InvestmentLab.Taxation.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Taxation.Declarations.Infrastructure;

internal sealed class Pit8CRepository(TaxationDbContext db) : IPit8CRepository
{
    public async Task<IReadOnlyList<Pit8CStatement>> ForYearAsync(int year, CancellationToken cancellationToken) =>
        await db.Pit8CStatements.Where(statement => statement.Year == year).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Pit8CStatement>> AllAsync(CancellationToken cancellationToken) =>
        await db.Pit8CStatements.ToListAsync(cancellationToken);

    public void Add(Pit8CStatement statement) => db.Pit8CStatements.Add(statement);

    public void Remove(Pit8CStatement statement) => db.Pit8CStatements.Remove(statement);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
