using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ImportPositions;

/// <summary>One position of the legacy <c>positions.json</c>.</summary>
/// <param name="Ticker">Yahoo symbol, e.g. <c>NQSE.DE</c>.</param>
/// <param name="EntryDate">Null: bought before dates were tracked; the import date is used.</param>
public sealed record LegacyPosition(string Ticker, decimal EntryPrice, decimal Shares, DateOnly? EntryDate, string Currency);

/// <param name="AsOf">Date for positions without an entry date. Null: today.</param>
public sealed record ImportPositionsCommand(Guid SubPortfolioId, decimal Cash, IReadOnlyList<LegacyPosition> Positions, DateOnly? AsOf = null);

/// <param name="Skipped">Entries already imported earlier (the import can be run again safely).</param>
public sealed record ImportReport(int Recorded, int Skipped, IReadOnlyList<string> Warnings);

/// <summary>
/// One-off migration of a sub-portfolio from <c>positions.json</c>: a contribution paying for everything,
/// then one buy per position at its entry price. Entries carry <c>import:</c> references, so a repeated import changes nothing.
/// </summary>
public sealed class ImportPositionsHandler(TransactionRecorder recorder, IInstrumentCatalog catalog, TimeProvider clock)
{
    private const string YahooProvider = "yfinance";

    public async Task<Result<ImportReport>> Handle(Guid accountId, ImportPositionsCommand command, CancellationToken cancellationToken)
    {
        if (command.SubPortfolioId == Guid.Empty)
            return TransactionErrors.UnknownSubPortfolio(command.SubPortfolioId);

        var instruments = await ResolveInstrumentsAsync(command.Positions, cancellationToken);
        if (instruments.IsFailure)
            return instruments.Error;

        var subPortfolioId = SubPortfolioId.Of(command.SubPortfolioId);
        var importDate = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var buys = command.Positions.Select(position => (Position: position, Date: position.EntryDate ?? importDate)).ToList();
        var reference = $"import:{command.SubPortfolioId}";

        var entries = new List<(string Reference, Func<RecordingContext, Result<Transaction>> Create)>
        {
            ($"{reference}:contribution", context =>
            {
                var currency = context.Account.BaseCurrency;
                var total = buys.Aggregate(Money.Of(command.Cash, currency),
                    (sum, buy) => sum + Price.Of(buy.Position.EntryPrice, currency).TotalFor(Quantity.Of(buy.Position.Shares)));
                var paidOn = buys.Count > 0 ? buys.Min(buy => buy.Date) : importDate;
                return total.Amount <= 0m
                    ? TransactionErrors.AmountNotPositive()
                    : Transaction.CashMovement(context.Account, CashMovementKind.Contribution, total, [new CashAllocation(subPortfolioId, total)], null,
                        paidOn, new TransactionSource(TransactionSourceKind.Import, $"{reference}:contribution"), context.Now);
            }),
        };

        foreach (var (position, date) in buys)
        {
            var source = new TransactionSource(TransactionSourceKind.Import, $"{reference}:{position.Ticker}");
            Result<Transaction> Buy(RecordingContext context)
            {
                var currency = Currency.TryOf(position.Currency);
                if (currency.IsFailure)
                    return currency.Error;
                var price = Price.TryOf(position.EntryPrice, currency.Value);
                var quantity = Quantity.TryOf(position.Shares);
                if (price.IsFailure || quantity.IsFailure)
                    return price.IsFailure ? price.Error : quantity.Error;

                return Transaction.Trade(context.Account, TradeSide.Buy, instruments.Value[position.Ticker], quantity.Value, price.Value,
                    Money.Zero(currency.Value), [new Allocation(subPortfolioId, quantity.Value)], date, source, context.Now);
            }
            entries.Add((source.Reference!, Buy));
        }

        var (recorded, skipped, warnings) = (0, 0, new List<string>());
        foreach (var (_, create) in entries)
        {
            var result = await recorder.RecordAsync(accountId, create, cancellationToken);
            if (result.IsSuccess)
            {
                recorded++;
                warnings = [.. result.Value.Warnings];
            }
            else if (result.Error.Code == "transaction.duplicate_source")
                skipped++;
            else
                return result.Error;
        }

        return new ImportReport(recorded, skipped, warnings);
    }

    private async Task<Result<IReadOnlyDictionary<string, Guid>>> ResolveInstrumentsAsync(IReadOnlyList<LegacyPosition> positions, CancellationToken cancellationToken)
    {
        var bySymbol = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var instrument in await catalog.GetInstruments(cancellationToken))
        {
            foreach (var symbol in instrument.ProviderSymbols.Where(symbol => symbol.StartsWith($"{YahooProvider}:", StringComparison.Ordinal)))
                bySymbol[symbol[(YahooProvider.Length + 1)..]] = instrument.Id;
        }

        var unknown = positions.Select(position => position.Ticker).Where(ticker => !bySymbol.ContainsKey(ticker)).ToList();
        if (unknown.Count > 0)
            return Error.Validation("import.unknown_tickers", $"No instrument has the Yahoo symbol {string.Join(", ", unknown)}.");
        if (positions.GroupBy(position => position.Ticker, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
            return Error.Validation("import.ticker_repeated", "Each ticker may appear only once.");

        return bySymbol;
    }
}
