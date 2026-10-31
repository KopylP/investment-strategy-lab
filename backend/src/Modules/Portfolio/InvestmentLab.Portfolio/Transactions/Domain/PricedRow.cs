namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record PricedRow(StatementRow Row, Guid? InstrumentId, Guid? SubPortfolioId, decimal Quantity, decimal Price, decimal Amount, Money? Settlement = null, decimal Fee = 0m,
    bool MissingRate = false)
{
    private const decimal RoundingNoise = 0.015m;

    public static decimal FeeOf(StatementRow row)
    {
        if (row.Kind is not (StatementRowKind.Buy or StatementRowKind.Sell) || row.Quantity is not { } quantity || row.Price is not { } price)
            return 0m;

        var gross = quantity * price;
        var fee = row.Kind == StatementRowKind.Buy ? row.Amount - gross : gross - row.Amount;

        return fee > RoundingNoise ? decimal.Round(fee, 2) : 0m;
    }
}
