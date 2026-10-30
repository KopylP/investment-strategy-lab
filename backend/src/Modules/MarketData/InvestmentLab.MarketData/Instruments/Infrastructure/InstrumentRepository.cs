using InvestmentLab.MarketData.Instruments.Application;
using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.MarketData.Instruments.Infrastructure;

internal sealed class InstrumentRepository(MarketDataDbContext db) : IInstrumentRepository
{
    public Task<Instrument?> FindAsync(InstrumentId id, CancellationToken cancellationToken) =>
        db.Instruments.FirstOrDefaultAsync(instrument => instrument.Id == id, cancellationToken);

    public Task<bool> IsinTakenAsync(Isin isin, CancellationToken cancellationToken) =>
        db.Instruments.AnyAsync(instrument => instrument.Isin == isin, cancellationToken);

    public async Task<InstrumentId?> FindOwnerAsync(ProviderSymbol symbol, CancellationToken cancellationToken)
    {
        var owners = await db.Instruments
            .Where(instrument => instrument.ProviderSymbols.Any(s => s.Provider == symbol.Provider && s.Symbol == symbol.Symbol))
            .Select(instrument => instrument.Id)
            .Take(1)
            .ToListAsync(cancellationToken);
        return owners is [var owner] ? owner : null;
    }

    public async Task<IReadOnlyList<Instrument>> ListAsync(CancellationToken cancellationToken) =>
        await db.Instruments.AsNoTracking().OrderBy(instrument => instrument.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Instrument>> ListForUpdateAsync(CancellationToken cancellationToken) =>
        await db.Instruments.ToListAsync(cancellationToken);

    public void Add(Instrument instrument) => db.Instruments.Add(instrument);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
