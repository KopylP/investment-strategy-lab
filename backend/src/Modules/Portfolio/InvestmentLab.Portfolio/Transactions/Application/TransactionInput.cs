using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Contracts.Transactions;
using InvestmentLab.Portfolio.Holdings.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Transactions.Application;

internal static class TransactionInput
{
    public static Result<TEnum> ParseEnum<TEnum>(string? text, string code) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) && Enum.IsDefined(value) && !int.TryParse(text, out _)
            ? value
            : Error.Validation(code, $"'{text}' is not one of {string.Join(", ", Enum.GetNames<TEnum>())}.");

    public static IReadOnlyList<(SubPortfolioId, decimal)> TargetWeights(InvestmentAccount account) =>
        [.. account.ActiveSubPortfolios.Select(sub => (sub.Id, sub.TargetShare.Value.Value))];

    public static IReadOnlyList<(SubPortfolioId, decimal)> HeldWeights(AccountHoldings holdings, Guid instrumentId) =>
        [.. holdings.SubPortfolios.Select(sub => (sub.SubPortfolioId, sub.UnitsOf(instrumentId).Value))];

    public static Result<IReadOnlyList<Allocation>> Quantities(IReadOnlyList<QuantityAllocationDto> inputs)
    {
        var allocations = new List<Allocation>();

        foreach (var input in inputs)
        {
            var quantity = Quantity.TryOf(input.Quantity);
            if (quantity.IsFailure || quantity.Value.IsNone)
                return TransactionErrors.QuantityNotPositive();

            if (input.SubPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(input.SubPortfolioId);

            allocations.Add(new Allocation(SubPortfolioId.Of(input.SubPortfolioId), quantity.Value));
        }

        return allocations;
    }

    public static Result<IReadOnlyList<CashAllocation>> Amounts(IReadOnlyList<AmountAllocationDto> inputs, Currency currency)
    {
        var allocations = new List<CashAllocation>();

        foreach (var input in inputs)
        {
            if (input.SubPortfolioId == Guid.Empty)
                return TransactionErrors.UnknownSubPortfolio(input.SubPortfolioId);

            allocations.Add(new CashAllocation(SubPortfolioId.Of(input.SubPortfolioId), Money.Of(input.Amount, currency)));
        }

        return allocations;
    }
}
