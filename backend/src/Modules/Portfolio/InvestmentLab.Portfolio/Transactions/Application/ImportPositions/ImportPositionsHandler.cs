using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Application.Shared;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ImportPositions;

public sealed record ImportPositionsCommand(Guid SubPortfolioId, decimal Cash, IReadOnlyList<LegacyPosition> Positions, DateOnly? AsOf = null);

public sealed class ImportPositionsHandler(TransactionRecorder recorder, IInstrumentCatalog catalog, TimeProvider clock)
{
    private const string DuplicateSourceCode = "transaction.duplicate_source";

    public async Task<Result<ImportReport>> Handle(Guid accountId, ImportPositionsCommand command, CancellationToken cancellationToken)
    {
        if (command.SubPortfolioId == Guid.Empty)
            return TransactionErrors.UnknownSubPortfolio(command.SubPortfolioId);

        var instruments = await ResolveInstrumentsAsync(command.Positions, cancellationToken);
        if (instruments.IsFailure)
            return instruments.Error;

        var importDate = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var buys = command.Positions.Select(position => BuyOf(position, importDate)).ToList();
        var import = new PositionImport(SubPortfolioId.Of(command.SubPortfolioId), command.Cash, buys, importDate);

        var entries = new List<Func<RecordingContext, Result<Transaction>>>
        {
            context => import.Contribution(context.Account, context.Now),
        };
        entries.AddRange(buys.Select<ImportedBuy, Func<RecordingContext, Result<Transaction>>>(
            buy => context => import.Buy(context.Account, buy, instruments.Value[buy.Ticker], context.Now)));

        return await RecordAllAsync(accountId, entries, cancellationToken);
    }

    private static ImportedBuy BuyOf(LegacyPosition position, DateOnly importDate) => new(
        position.Ticker,
        position.Shares,
        position.EntryPrice,
        position.Currency,
        position.EntryDate ?? importDate);

    private async Task<Result<ImportReport>> RecordAllAsync(
        Guid accountId, List<Func<RecordingContext, Result<Transaction>>> entries, CancellationToken cancellationToken)
    {
        var (recorded, skipped, warnings) = (0, 0, new List<string>());

        foreach (var create in entries)
        {
            var result = await recorder.RecordAsync(accountId, create, cancellationToken);
            if (result.IsSuccess)
            {
                recorded++;
                warnings = [.. result.Value.Warnings];
            }
            else if (result.Error.Code == DuplicateSourceCode)
                skipped++;
            else
                return result.Error;
        }

        return new ImportReport(recorded, skipped, warnings);
    }

    private async Task<Result<IReadOnlyDictionary<string, Guid>>> ResolveInstrumentsAsync(
        IReadOnlyList<LegacyPosition> positions,
        CancellationToken cancellationToken)
    {
        var bySymbol = await InstrumentsByTickerAsync(cancellationToken);

        var unknown = positions.Select(position => position.Ticker).Where(ticker => !bySymbol.ContainsKey(ticker)).ToList();
        if (unknown.Count > 0)
            return Error.Validation("import.unknown_tickers", $"No instrument has the ticker {string.Join(", ", unknown)}.", new { tickers = string.Join(", ", unknown) });

        if (PositionImport.TickersRepeat(positions.Select(position => position.Ticker)))
            return Error.Validation("import.ticker_repeated", "Each ticker may appear only once.");

        return bySymbol;
    }

    private async Task<Dictionary<string, Guid>> InstrumentsByTickerAsync(CancellationToken cancellationToken)
    {
        var bySymbol = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var instrument in await catalog.GetInstruments(cancellationToken))
        {
            if (instrument.Ticker is { } ticker)
                bySymbol[ticker] = instrument.Id;
        }

        return bySymbol;
    }
}
