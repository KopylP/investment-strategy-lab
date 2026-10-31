using InvestmentLab.Portfolio.Accounts.Domain;

namespace InvestmentLab.Portfolio.Holdings.Domain;

public sealed record SubPortfolioHoldings(
    SubPortfolioId SubPortfolioId,
    Money Cash,
    IReadOnlyList<Money> ForeignCash,
    IReadOnlyList<HoldingLine> Lines)
{
    public Quantity UnitsOf(Guid instrumentId) => Lines.FirstOrDefault(line => line.InstrumentId == instrumentId)?.Quantity ?? Quantity.None;
}
