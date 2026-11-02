using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;
using Microsoft.Extensions.Caching.Memory;

namespace InvestmentLab.MarketData.Quotes.Infrastructure;

internal sealed class QuoteArchiveCache : IDisposable
{
    private const long PricesKept = 500_000;
    private static readonly TimeSpan IdleLifetime = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(6);

    private readonly MemoryCache _prices = new(new MemoryCacheOptions { SizeLimit = PricesKept });
    private readonly SemaphoreSlim _loading = new(1, 1);

    public async Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> GetOrLoadAsync(
        IReadOnlyCollection<InstrumentId> instrumentIds,
        Func<IReadOnlyList<InstrumentId>, CancellationToken, Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>>> load,
        CancellationToken cancellationToken)
    {
        var found = Found(instrumentIds);
        if (found.Count == instrumentIds.Count)
            return found;

        await _loading.WaitAsync(cancellationToken);
        try
        {
            return await LoadMissingAsync(instrumentIds, load, cancellationToken);
        }
        finally
        {
            _loading.Release();
        }
    }

    public void Dispose()
    {
        _prices.Dispose();
        _loading.Dispose();
    }

    private async Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>> LoadMissingAsync(
        IReadOnlyCollection<InstrumentId> instrumentIds,
        Func<IReadOnlyList<InstrumentId>, CancellationToken, Task<IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>>> load,
        CancellationToken cancellationToken)
    {
        var found = Found(instrumentIds);
        var missing = instrumentIds.Where(id => !found.ContainsKey(id)).ToList();
        if (missing.Count == 0)
            return found;

        var loaded = await load(missing, cancellationToken);
        Keep(loaded);

        foreach (var (instrumentId, prices) in loaded)
            found[instrumentId] = prices;

        return found;
    }

    private Dictionary<InstrumentId, IReadOnlyList<ArchivedPrice>> Found(IReadOnlyCollection<InstrumentId> instrumentIds)
    {
        var found = new Dictionary<InstrumentId, IReadOnlyList<ArchivedPrice>>();

        foreach (var instrumentId in instrumentIds)
        {
            if (_prices.TryGetValue(instrumentId, out IReadOnlyList<ArchivedPrice>? prices) && prices is not null)
                found[instrumentId] = prices;
        }

        return found;
    }

    private void Keep(IReadOnlyDictionary<InstrumentId, IReadOnlyList<ArchivedPrice>> loaded)
    {
        foreach (var (instrumentId, prices) in loaded)
        {
            var entry = new MemoryCacheEntryOptions
            {
                Size = Math.Max(1, prices.Count),
                SlidingExpiration = IdleLifetime,
                AbsoluteExpirationRelativeToNow = Lifetime,
            };

            _prices.Set(instrumentId, prices, entry);
        }
    }
}
