using InvestmentLab.MarketData.Instruments.Application.Shared;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;

public sealed class DescribeInstrumentsHandler(IInstrumentRepository instruments, ISymbolLookup lookup, TimeProvider clock) : IHandler
{
    private const int SymbolsPerRequest = 10;

    public async Task<int> Handle(IReadOnlyCollection<Guid>? only, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = (await instruments.ListForUpdateAsync(cancellationToken))
            .Where(instrument => only is null ? instrument.NeedsDescription(now) : only.Contains(instrument.Id.Value))
            .Select(instrument => (Instrument: instrument, Symbol: instrument.Ticker))
            .Where(item => item.Symbol is not null)
            .ToList();

        var described = 0;

        foreach (var chunk in due.Chunk(SymbolsPerRequest))
        {
            var matches = await lookup.LookupAsync([.. chunk.Select(item => new SymbolQuery([item.Symbol!], null))], cancellationToken);
            foreach (var (item, match) in chunk.Zip(matches))
            {
                if (match is null)
                    continue;

                item.Instrument.Describe(match.Description, now);
                described++;
            }

            await instruments.SaveChangesAsync(cancellationToken);
        }

        return described;
    }
}
