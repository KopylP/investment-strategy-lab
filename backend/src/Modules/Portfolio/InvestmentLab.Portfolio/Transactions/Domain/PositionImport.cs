using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record PositionImport(SubPortfolioId SubPortfolioId, decimal Cash, IReadOnlyList<ImportedBuy> Buys, DateOnly ImportDate)
{
    private string Reference => $"import:{SubPortfolioId.Value}";

    public static bool TickersRepeat(IEnumerable<string> tickers) =>
        tickers.GroupBy(ticker => ticker, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1);

    public Result<Transaction> Contribution(InvestmentAccount account, DateTimeOffset now)
    {
        var currency = account.BaseCurrency;
        var total = Buys.Aggregate(Money.Of(Cash, currency), (sum, buy) => sum + CostOf(buy, currency));

        if (total.Amount <= 0m)
            return TransactionErrors.AmountNotPositive();

        var paidOn = Buys.Count > 0 ? Buys.Min(buy => buy.Date) : ImportDate;
        var source = new TransactionSource(TransactionSourceKind.Import, $"{Reference}:contribution");
        CashAllocation[] allocations = [new CashAllocation(SubPortfolioId, total)];

        return Transaction.CashMovement(account, CashMovementKind.Contribution, total, allocations, null, paidOn, source, now);
    }

    public Result<Transaction> Buy(InvestmentAccount account, ImportedBuy buy, Guid instrumentId, DateTimeOffset now)
    {
        var currency = Currency.TryOf(buy.Currency);
        if (currency.IsFailure)
            return currency.Error;

        var price = Price.TryOf(buy.EntryPrice, currency.Value);
        if (price.IsFailure)
            return price.Error;

        var quantity = Quantity.TryOf(buy.Shares);
        if (quantity.IsFailure)
            return quantity.Error;

        var source = new TransactionSource(TransactionSourceKind.Import, $"{Reference}:{buy.Ticker}");
        Allocation[] allocations = [new Allocation(SubPortfolioId, quantity.Value)];

        return Transaction.Trade(
            account,
            TradeSide.Buy,
            instrumentId,
            quantity.Value,
            price.Value,
            Money.Zero(currency.Value),
            allocations,
            buy.Date,
            source,
            now);
    }

    private static Money CostOf(ImportedBuy buy, Currency currency) =>
        Price.Of(buy.EntryPrice, currency).TotalFor(Quantity.Of(buy.Shares));
}
