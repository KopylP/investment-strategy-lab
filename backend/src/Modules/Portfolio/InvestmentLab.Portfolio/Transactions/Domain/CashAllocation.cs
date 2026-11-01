using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Transactions.Domain;

public sealed record CashAllocation(SubPortfolioId SubPortfolioId, Money Amount)
{
    public static Result<IReadOnlyList<CashAllocation>> TryAll(IEnumerable<(Guid SubPortfolioId, decimal Amount)> given, Currency currency)
    {
        var allocations = new List<CashAllocation>();

        foreach (var (subPortfolioId, amount) in given)
        {
            if (subPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(subPortfolioId);

            allocations.Add(new CashAllocation(SubPortfolioId.Of(subPortfolioId), Money.Of(amount, currency)));
        }

        return allocations;
    }
}
