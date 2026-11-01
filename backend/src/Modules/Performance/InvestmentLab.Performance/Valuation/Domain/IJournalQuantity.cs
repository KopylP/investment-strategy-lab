namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalQuantity
{
    Guid SubPortfolioId { get; }
    decimal Quantity { get; }
}
