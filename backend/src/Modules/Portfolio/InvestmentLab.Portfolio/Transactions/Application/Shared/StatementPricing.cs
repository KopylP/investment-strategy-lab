using System.Security.Cryptography;
using System.Text;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application.Shared;

public sealed class StatementPricing(IFxRates fxRates)
{
    public async Task<IReadOnlyList<PricedRow>> PriceAsync(InvestmentAccount account, StatementImport statement, CancellationToken cancellationToken)
    {
        var accountCurrency = account.BaseCurrency.Code;
        var rates = new Dictionary<(string, DateOnly), decimal?>();
        var priced = new List<PricedRow>();

        foreach (var row in statement.Rows)
        {
            var factor = await FactorOfAsync(row, accountCurrency, rates, cancellationToken);

            priced.Add(PricedRowOf(row, statement, accountCurrency, factor));
        }

        return priced;
    }

    private async Task<decimal> FactorOfAsync(
        StatementRow row, string accountCurrency, Dictionary<(string, DateOnly), decimal?> rates, CancellationToken cancellationToken)
    {
        if (IsInAccountCurrency(row, accountCurrency))
            return 1m;

        var key = (row.Currency, row.OccurredOn);

        if (!rates.TryGetValue(key, out var rate))
            rate = rates[key] = await fxRates.GetCrossRate(row.Currency, accountCurrency, row.OccurredOn, cancellationToken);

        return rate ?? 0m;
    }

    private static PricedRow PricedRowOf(StatementRow row, StatementImport statement, string accountCurrency, decimal factor) => new(
        row,
        InstrumentOf(row, statement),
        SubPortfolioOf(row, statement),
        row.Quantity ?? 0m,
        (row.Price ?? 0m) * factor,
        row.Amount * factor,
        SettlementOf(row, accountCurrency, factor),
        PricedRow.FeeOf(row) * factor,
        MissingRate: factor == 0m);

    private static Guid? InstrumentOf(StatementRow row, StatementImport statement)
    {
        var mapping = statement.MappingOf(row.Ticker);

        return mapping?.InstrumentId ?? (mapping?.RegisterAs is { Length: > 0 } symbol ? Placeholder(symbol) : null);
    }

    private static Guid? SubPortfolioOf(StatementRow row, StatementImport statement)
    {
        if (!row.NeedsInstrument)
            return statement.CashSubPortfolioId;

        var mapped = statement.MappingOf(row.Ticker)?.SubPortfolioId;

        return mapped ?? (row.Kind == StatementRowKind.Dividend ? statement.CashSubPortfolioId : null);
    }

    private static Money? SettlementOf(StatementRow row, string accountCurrency, decimal factor)
    {
        var settlesInOwnCurrency = !IsInAccountCurrency(row, accountCurrency)
            && factor != 0m
            && row.Amount > 0m
            && row.Kind != StatementRowKind.Unsupported;

        return settlesInOwnCurrency && Currency.TryOf(row.Currency) is { IsSuccess: true } own
            ? Money.Of(row.Amount, own.Value)
            : null;
    }

    private static bool IsInAccountCurrency(StatementRow row, string accountCurrency) =>
        string.Equals(row.Currency, accountCurrency, StringComparison.OrdinalIgnoreCase);

    private static Guid Placeholder(string symbol) => new(MD5.HashData(Encoding.UTF8.GetBytes($"new:{symbol}")));
}
