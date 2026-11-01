using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record Allocation(SubPortfolioId SubPortfolioId, Quantity Quantity)
{
    public static Result<IReadOnlyList<Allocation>> TryAll(IEnumerable<(Guid SubPortfolioId, decimal Quantity)> given)
    {
        var allocations = new List<Allocation>();

        foreach (var (subPortfolioId, units) in given)
        {
            var quantity = Quantity.TryOf(units);
            if (quantity.IsFailure || quantity.Value.IsNone)
                return TransactionErrors.QuantityNotPositive();

            if (subPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(subPortfolioId);

            allocations.Add(new Allocation(SubPortfolioId.Of(subPortfolioId), quantity.Value));
        }

        return allocations;
    }
}
