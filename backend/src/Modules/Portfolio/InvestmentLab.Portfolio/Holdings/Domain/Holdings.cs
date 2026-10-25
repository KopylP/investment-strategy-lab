using InvestmentLab.Portfolio.Accounts.Domain;
using InvestmentLab.Portfolio.Transactions.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

/// <summary>Units bought by one transaction that a sub-portfolio still holds; unit cost includes the fee share.</summary>
public sealed record VirtualLot(TransactionId OpenedBy, DateOnly OpenedOn, Quantity Quantity, decimal UnitCost)
{
    public decimal Cost => Quantity.Value * UnitCost;
}

public sealed record HoldingLine(Guid InstrumentId, IReadOnlyList<VirtualLot> Lots)
{
    public Quantity Quantity => Quantity.Of(Lots.Sum(lot => lot.Quantity.Value));

    public decimal CostBasis => decimal.Round(Lots.Sum(lot => lot.Cost), 2);

    public decimal AverageCost => Quantity.IsNone ? 0m : Lots.Sum(lot => lot.Cost) / Quantity.Value;

    public DateOnly FirstOpenedOn => Lots.Min(lot => lot.OpenedOn);
}

public sealed record SubPortfolioHoldings(SubPortfolioId SubPortfolioId, Money Cash, IReadOnlyList<HoldingLine> Lines)
{
    public Quantity UnitsOf(Guid instrumentId) => Lines.FirstOrDefault(line => line.InstrumentId == instrumentId)?.Quantity ?? Quantity.None;
}

/// <summary>What every sub-portfolio of an account holds after the journal up to <see cref="AsOf"/>.</summary>
public sealed record AccountHoldings(AccountId AccountId, Currency Currency, DateOnly? AsOf, IReadOnlyList<SubPortfolioHoldings> SubPortfolios)
{
    public SubPortfolioHoldings? For(SubPortfolioId subPortfolioId) => SubPortfolios.FirstOrDefault(sub => sub.SubPortfolioId == subPortfolioId);

    public Quantity UnitsOf(SubPortfolioId subPortfolioId, Guid instrumentId) => For(subPortfolioId)?.UnitsOf(instrumentId) ?? Quantity.None;
}

/// <param name="Warnings">Not errors: e.g. a buy recorded before the contribution that paid for it leaves cash below zero.</param>
public sealed record ProjectedHoldings(AccountHoldings Holdings, IReadOnlyList<string> Warnings);
