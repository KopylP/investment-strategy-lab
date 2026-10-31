namespace InvestmentLab.Performance.Contracts;

public interface IValuationHistory
{
    Task<IReadOnlyList<TwrPointDto>> GetTwrHistory(Guid accountId, Guid? subPortfolioId, DateOnly from, CancellationToken cancellationToken);
}
