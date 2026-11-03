using InvestmentLab.Signals.Monitoring.Domain;
using InvestmentLab.Signals.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class SignalRepository(SignalsDbContext db) : ISignalRepository
{
    private IQueryable<SignalOccurrence> OpenOccurrences =>
        db.Occurrences.Where(occurrence => occurrence.ClearedAt == null);

    public Task<Signal?> FindAsync(Guid signalId, CancellationToken cancellationToken) =>
        db.Signals.SingleOrDefaultAsync(signal => signal.Id == signalId, cancellationToken);

    public async Task<IReadOnlyList<Signal>> AllAsync(CancellationToken cancellationToken) =>
        await db.Signals.ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> BuiltInKeysAsync(CancellationToken cancellationToken)
    {
        var keys = await db.Signals
            .IgnoreQueryFilters()
            .Where(signal => signal.BuiltInKey != null)
            .Select(signal => signal.BuiltInKey!)
            .ToListAsync(cancellationToken);

        return keys.ToHashSet();
    }

    public async Task<IReadOnlyList<SignalOccurrence>> OpenOccurrencesOfAsync(Guid signalId, CancellationToken cancellationToken) =>
        await OpenOccurrences
            .Where(occurrence => occurrence.SignalId == signalId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalOccurrence>> OpenOccurrencesOfAsync(IReadOnlyList<Guid> signalIds, CancellationToken cancellationToken) =>
        await OpenOccurrences
            .Where(occurrence => signalIds.Contains(occurrence.SignalId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalOccurrence>> AllOpenOccurrencesAsync(CancellationToken cancellationToken) =>
        await OpenOccurrences
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SignalOccurrence>> ClearedOccurrencesAsync(int count, CancellationToken cancellationToken) =>
        await db.Occurrences
            .AsNoTracking()
            .Where(occurrence => occurrence.ClearedAt != null)
            .OrderByDescending(occurrence => occurrence.TriggeredAt)
            .Take(count)
            .ToListAsync(cancellationToken);

    public void Add(Signal signal) => db.Signals.Add(signal);

    public void Add(SignalOccurrence occurrence) => db.Occurrences.Add(occurrence);

    public void Remove(Signal signal) => db.Signals.Remove(signal);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
