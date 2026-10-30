using InvestmentLab.Strategies.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

/// <summary>Keeps the stored catalog in step with the engines: synced when empty or older than an hour; a stale copy beats no copy.</summary>
public sealed class CatalogSynchronizer(StrategiesDbContext db, StrategyEngineClient engine, TimeProvider clock, ILogger<CatalogSynchronizer> logger)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    public async Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken)
    {
        var stored = await db.StrategyTypes.OrderBy(type => type.Code).ToListAsync(cancellationToken);
        if (stored.Count > 0 && stored.All(type => clock.GetUtcNow() - type.SyncedAt < MaxAge))
            return stored;

        try
        {
            await SyncAsync(cancellationToken);
            return await db.StrategyTypes.OrderBy(type => type.Code).ToListAsync(cancellationToken);
        }
        catch (Exception exception) when (stored.Count > 0 && exception is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            logger.LogWarning(exception, "Strategy engine unavailable; using the catalog synced at {SyncedAt}", stored.Min(type => type.SyncedAt));
            return stored;
        }
    }

    public async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var published = (await engine.GetStrategyTypesAsync(cancellationToken))
            .Select(type => StrategyTypeDescriptor.Of(type.Code, type.Name, type.Description, type.Engine, type.Version, type.SupportsState,
                type.RequiredSeriesRoles, type.ParametersSchema.GetRawText(), type.DefaultParameters.GetRawText(), type.Reports ?? [], now))
            .ToList();

        var stored = await db.StrategyTypes.ToDictionaryAsync(type => type.Code, cancellationToken);
        foreach (var type in published)
        {
            if (stored.TryGetValue(type.Code, out var existing))
                existing.RefreshFrom(type);
            else
                db.StrategyTypes.Add(type);
        }

        await db.SaveChangesAsync(cancellationToken);
        return stored.Count + published.Count(type => !stored.ContainsKey(type.Code));
    }
}
