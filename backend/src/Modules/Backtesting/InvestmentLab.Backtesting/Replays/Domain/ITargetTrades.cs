namespace InvestmentLab.Backtesting.Replays.Domain;

public interface ITargetTrades
{
    IReadOnlyList<ReplayTrade> TradesFor(ReplayDecision decision, ReplaySetup setup, ReplayBook book, IReadOnlyDictionary<Guid, decimal> prices);
}
