namespace InvestmentLab.Signals.Contracts;

public interface IPortfolioReturnHistory
{
    Task<IReadOnlyList<TwrPointDto>> GetTwrHistory(Guid accountId, Guid? subPortfolioId, DateOnly from, CancellationToken cancellationToken);
}
