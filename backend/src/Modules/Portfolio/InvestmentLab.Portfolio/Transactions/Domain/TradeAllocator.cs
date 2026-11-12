using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public static class TradeAllocator
{
    private const int QuantityDecimals = 8;

    public static IReadOnlyList<Allocation> Split(Quantity total, IReadOnlyList<(SubPortfolioId SubPortfolioId, decimal Weight)> weights) =>
        [.. SplitValue(
            total.Value,
            weights,
            QuantityDecimals).Where(part => part.Value != 0m).Select(part => new Allocation(part.SubPortfolioId, Quantity.Of(part.Value)))];

    public static IReadOnlyList<CashAllocation> Split(Money total, IReadOnlyList<(SubPortfolioId SubPortfolioId, decimal Weight)> weights) =>
        [.. SplitValue(
            total.Amount,
            weights,
            2).Where(part => part.Value != 0m).Select(part => new CashAllocation(part.SubPortfolioId, Money.Of(part.Value, total.Currency)))];

    public static IReadOnlyList<Money> SplitFee(Money fee, IReadOnlyList<Allocation> allocations) =>
        [.. SplitValue(
            fee.Amount,
            [.. allocations.Select(a => (a.SubPortfolioId, a.Quantity.Value))],
            2).Select(part => Money.Of(part.Value, fee.Currency))];

    private static List<(SubPortfolioId SubPortfolioId, decimal Value)> SplitValue(
        decimal total, IReadOnlyList<(SubPortfolioId SubPortfolioId, decimal Weight)> weights, int decimals)
    {
        var positive = weights.Where(w => w.Weight > 0m).ToList();
        if (positive.Count == 0)
            return [];

        if (total == 0m)
            return [.. positive.Select(w => (w.SubPortfolioId, 0m))];

        var weightSum = positive.Sum(w => w.Weight);
        var parts =
            positive.Select(w => (w.SubPortfolioId, Value: decimal.Round(total * w.Weight / weightSum, decimals, MidpointRounding.ToZero))).ToList();

        var largest = positive.IndexOf(positive.MaxBy(w => w.Weight));
        parts[largest] = (parts[largest].SubPortfolioId, parts[largest].Value + total - parts.Sum(p => p.Value));

        return parts;
    }
}
