using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed class CatalogSynchronizer(StrategiesDbContext db, StrategyEngineClient engine, TimeProvider clock, ILogger<CatalogSynchronizer> logger)
{
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(1);

    private static readonly SemaphoreSlim OneSyncAtATime = new(1, 1);

    public async Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken)
    {
        var stored = await db.StrategyTypes.OrderBy(type => type.Code).ToListAsync(cancellationToken);
        if (stored.Count > 0 && stored.All(type => clock.GetUtcNow() - type.SyncedAt < MaxAge))
            return stored;

        return await SyncedOrStoredAsync(stored, cancellationToken);
    }

    public async Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken)
    {
        var listed = TypeOf(code, await ListAsync(cancellationToken));
        if (listed is not null)
            return listed;

        var published = await ListFromEngineAsync(cancellationToken);

        return TypeOf(code, published);
    }

    public async Task<IReadOnlyList<StrategyTypeDescriptor>> ListFromEngineAsync(CancellationToken cancellationToken)
    {
        var stored = await db.StrategyTypes.OrderBy(type => type.Code).ToListAsync(cancellationToken);

        return await SyncedOrStoredAsync(stored, cancellationToken);
    }

    private async Task<IReadOnlyList<StrategyTypeDescriptor>> SyncedOrStoredAsync(
        List<StrategyTypeDescriptor> stored,
        CancellationToken cancellationToken)
    {
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
        await OneSyncAtATime.WaitAsync(cancellationToken);
        try
        {
            return await SyncWithEngineAsync(cancellationToken);
        }
        finally
        {
            OneSyncAtATime.Release();
        }
    }

    private async Task<int> SyncWithEngineAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var published = (await engine.GetStrategyTypesAsync(cancellationToken))
            .Select(type => StrategyTypeDescriptor.Of(type.Code, type.Name, type.Description, type.Engine, type.Version, type.SupportsState,
                type.RequiredSeriesRoles, type.ParametersSchema.GetRawText(), RawDefaults(type), type.Reports ?? [], now, type.Messages, type.SupportsBacktest, type.BacktestComparisons))
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

    private static StrategyTypeDescriptor? TypeOf(string code, IReadOnlyList<StrategyTypeDescriptor> types) =>
        types.FirstOrDefault(type => type.Code == code);

    private static string? RawDefaults(EngineStrategyType type) =>
        type.DefaultParameters is { ValueKind: JsonValueKind.Object } defaults ? defaults.GetRawText() : null;
}
