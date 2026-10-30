using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.DescribeInstruments;

/// <summary>
/// Asks Yahoo what each instrument is: those never asked and those asked longer than <see cref="Instrument.DescriptionLifetime"/>
/// ago (or only the given ones, e.g. one just registered). An instrument Yahoo does not answer for (unknown symbol, engine down)
/// is asked again next time.
/// </summary>
public sealed class DescribeInstrumentsHandler(IInstrumentRepository instruments, ISymbolLookup lookup, TimeProvider clock)
{
    // Yahoo answers one symbol at a time: a batch stays well inside the lookup's timeout.
    private const int SymbolsPerRequest = 10;

    /// <returns>How many instruments got an answer.</returns>
    public async Task<int> Handle(IReadOnlyCollection<InstrumentId>? only, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var due = (await instruments.ListForUpdateAsync(cancellationToken))
            .Where(instrument => only is null ? instrument.NeedsDescription(now) : only.Contains(instrument.Id))
            .Select(instrument => (Instrument: instrument, Symbol: instrument.SymbolFor(QuoteProvider.YFinance)?.Symbol))
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
