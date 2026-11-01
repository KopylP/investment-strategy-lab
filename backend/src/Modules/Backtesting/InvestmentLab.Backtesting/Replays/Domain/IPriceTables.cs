namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IPriceTables
{
    Task<PriceTable> LoadAsync(ReplaySetup setup, CancellationToken cancellationToken);
}
