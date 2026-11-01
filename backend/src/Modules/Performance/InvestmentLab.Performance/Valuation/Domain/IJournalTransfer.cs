namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalTransfer
{
    Guid FromSubPortfolioId { get; }
    Guid ToSubPortfolioId { get; }
    Guid? InstrumentId { get; }
    decimal? Quantity { get; }
    decimal? Amount { get; }
}
