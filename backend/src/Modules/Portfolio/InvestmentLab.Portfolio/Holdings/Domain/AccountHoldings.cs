using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record AccountHoldings(AccountId AccountId, Currency Currency, DateOnly? AsOf, IReadOnlyList<SubPortfolioHoldings> SubPortfolios)
{
    public IReadOnlyList<(SubPortfolioId, decimal)> WeightsOf(Guid instrumentId) =>
        [.. SubPortfolios.Select(sub => (sub.SubPortfolioId, sub.UnitsOf(instrumentId).Value))];

    public SubPortfolioHoldings? For(SubPortfolioId subPortfolioId) => SubPortfolios.FirstOrDefault(sub => sub.SubPortfolioId == subPortfolioId);

    public Quantity UnitsOf(SubPortfolioId subPortfolioId, Guid instrumentId) => For(subPortfolioId)?.UnitsOf(instrumentId) ?? Quantity.None;
}
