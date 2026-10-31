namespace InvestmentLab.MarketData.Contracts.FxRates;

public interface IFxRates
{
    Task<FxRateDto?> GetFxRate(string baseCurrency, string quoteCurrency, DateOnly transactionDate, CancellationToken cancellationToken);

    Task<IReadOnlyList<FxRateDto>> GetFxRateHistory(string baseCurrency, string quoteCurrency, DateOnly from, DateOnly to, CancellationToken cancellationToken);
}
