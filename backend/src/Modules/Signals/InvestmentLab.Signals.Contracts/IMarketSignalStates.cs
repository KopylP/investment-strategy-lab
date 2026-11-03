namespace InvestmentLab.Signals.Contracts;

public interface IMarketSignalStates
{
    Task<IReadOnlyList<MarketSignalStateDto>> GetStatesAsync(IReadOnlyList<Guid> signalIds, CancellationToken cancellationToken);
}
