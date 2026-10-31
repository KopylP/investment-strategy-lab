using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.ImportPositions;

public sealed record ImportPositionsCommand(Guid SubPortfolioId, decimal Cash, IReadOnlyList<LegacyPosition> Positions, DateOnly? AsOf = null);

public sealed class ImportPositionsHandler(TransactionRecorder recorder, IInstrumentCatalog catalog, TimeProvider clock)
{
    private const string YahooProvider = "yfinance";
    private const string DuplicateSourceCode = "transaction.duplicate_source";

    public async Task<Result<ImportReport>> Handle(Guid accountId, ImportPositionsCommand command, CancellationToken cancellationToken)
    {
        if (command.SubPortfolioId == Guid.Empty)
            return TransactionErrors.UnknownSubPortfolio(command.SubPortfolioId);

        var instruments = await ResolveInstrumentsAsync(command.Positions, cancellationToken);
        if (instruments.IsFailure)
            return instruments.Error;

        var importDate = command.AsOf ?? DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var buys = command.Positions.Select(position => new PositionBuy(position, position.EntryDate ?? importDate)).ToList();
        var target = new ImportTarget(SubPortfolioId.Of(command.SubPortfolioId), $"import:{command.SubPortfolioId}");

        var entries = new List<Func<RecordingContext, Result<Transaction>>>
        {
            context => Contribution(context, target, command.Cash, buys, importDate),
        };
        entries.AddRange(buys.Select<PositionBuy, Func<RecordingContext, Result<Transaction>>>(
            buy => context => Buy(context, target, buy, instruments.Value[buy.Position.Ticker])));

        return await RecordAllAsync(accountId, entries, cancellationToken);
    }

    private static Result<Transaction> Contribution(
        RecordingContext context, ImportTarget target, decimal cash, List<PositionBuy> buys, DateOnly importDate)
    {
        var currency = context.Account.BaseCurrency;
        var total = buys.Aggregate(Money.Of(cash, currency), (sum, buy) => sum + CostOf(buy, currency));
        if (total.Amount <= 0m)
            return TransactionErrors.AmountNotPositive();

        var paidOn = buys.Count > 0 ? buys.Min(buy => buy.Date) : importDate;
        var source = new TransactionSource(TransactionSourceKind.Import, $"{target.Reference}:contribution");
        CashAllocation[] allocations = [new CashAllocation(target.SubPortfolioId, total)];

        return Transaction.CashMovement(
            context.Account, CashMovementKind.Contribution, total, allocations, null, paidOn, source, context.Now);
    }

    private static Money CostOf(PositionBuy buy, Currency currency)
    {
        var price = Price.Of(buy.Position.EntryPrice, currency);

        return price.TotalFor(Quantity.Of(buy.Position.Shares));
    }

    private static Result<Transaction> Buy(RecordingContext context, ImportTarget target, PositionBuy buy, Guid instrumentId)
    {
        var position = buy.Position;

        var currency = Currency.TryOf(position.Currency);
        if (currency.IsFailure)
            return currency.Error;

        var price = Price.TryOf(position.EntryPrice, currency.Value);
        if (price.IsFailure)
            return price.Error;

        var quantity = Quantity.TryOf(position.Shares);
        if (quantity.IsFailure)
            return quantity.Error;

        var source = new TransactionSource(TransactionSourceKind.Import, $"{target.Reference}:{position.Ticker}");
        Allocation[] allocations = [new Allocation(target.SubPortfolioId, quantity.Value)];

        return Transaction.Trade(
            context.Account,
            TradeSide.Buy,
            instrumentId,
            quantity.Value,
            price.Value,
            Money.Zero(currency.Value),
            allocations,
            buy.Date,
            source,
            context.Now);
    }

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

    private async Task<Result<IReadOnlyDictionary<string, Guid>>> ResolveInstrumentsAsync(IReadOnlyList<LegacyPosition> positions, CancellationToken cancellationToken)
    {
        var bySymbol = await InstrumentsByYahooSymbolAsync(cancellationToken);

        var unknown = positions.Select(position => position.Ticker).Where(ticker => !bySymbol.ContainsKey(ticker)).ToList();
        if (unknown.Count > 0)
            return Error.Validation("import.unknown_tickers", $"No instrument has the Yahoo symbol {string.Join(", ", unknown)}.");

        var byTicker = positions.GroupBy(position => position.Ticker, StringComparer.OrdinalIgnoreCase);
        if (byTicker.Any(group => group.Count() > 1))
            return Error.Validation("import.ticker_repeated", "Each ticker may appear only once.");

        return bySymbol;
    }

    private async Task<Dictionary<string, Guid>> InstrumentsByYahooSymbolAsync(CancellationToken cancellationToken)
    {
        var prefix = $"{YahooProvider}:";
        var bySymbol = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var instrument in await catalog.GetInstruments(cancellationToken))
        {
            var yahooSymbols = instrument.ProviderSymbols.Where(symbol => symbol.StartsWith(prefix, StringComparison.Ordinal));

            foreach (var symbol in yahooSymbols)
                bySymbol[symbol[prefix.Length..]] = instrument.Id;
        }

        return bySymbol;
    }

    private sealed record PositionBuy(LegacyPosition Position, DateOnly Date);

    private sealed record ImportTarget(SubPortfolioId SubPortfolioId, string Reference);
}
