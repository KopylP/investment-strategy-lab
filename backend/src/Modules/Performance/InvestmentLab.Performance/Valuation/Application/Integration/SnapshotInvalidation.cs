using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class SnapshotInvalidation(IPerformanceRepository store)
{
    public async Task MarkAccountAsync(Guid accountId, Guid ownerId, DateOnly from, CancellationToken cancellationToken)
    {
        var marker = await store.MarkerAsync(accountId, UserId.Of(ownerId), cancellationToken);
        marker.MarkDirty(from);

        await store.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAccountsAsync(DateOnly from, CancellationToken cancellationToken)
    {
        var markers = await store.MarkersAsync(cancellationToken);
        foreach (var marker in markers)
            marker.MarkDirty(from);

        await store.SaveChangesAsync(cancellationToken);
    }
}
