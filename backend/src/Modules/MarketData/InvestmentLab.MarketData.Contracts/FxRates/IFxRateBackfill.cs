namespace InvestmentLab.MarketData.Contracts.FxRates;

public interface IFxRateBackfill
{
    Task<bool> EnsureSinceAsync(DateOnly since, CancellationToken cancellationToken);
}
