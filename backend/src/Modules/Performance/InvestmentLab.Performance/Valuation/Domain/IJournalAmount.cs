namespace InvestmentLab.Performance.Valuation.Domain;

public interface IJournalAmount
{
    Guid SubPortfolioId { get; }
    decimal Amount { get; }
}
