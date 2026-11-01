using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class TargetTrades(IRebalancingRules rules) : ITargetTrades
{
    public IReadOnlyList<ReplayTrade> TradesFor(ReplayDecision decision, ReplaySetup setup, ReplayBook book, IReadOnlyDictionary<Guid, decimal> prices)
    {
        var ask = new RebalancingAskDto(
            MarketIsFavorable: decision.MarketIsFavorable,
            InvestFraction: decision.InvestFraction,
            Targets: [.. decision.Targets.Select(target => new RebalancingTargetDto(target.InstrumentId, target.Weight))],
            Positions: [.. book.Positions.Select(position => new RebalancingPositionDto(position.InstrumentId, position.Quantity, position.AverageCost))],
            Cash: book.Cash,
            Prices: prices,
            CashInstrumentId: setup.CashInstrumentId,
            Currency: setup.Currency);

        return [.. rules.TradesFor(ask).Select(trade => new ReplayTrade(trade.InstrumentId, trade.IsSale, trade.Quantity, trade.Amount))];
    }
}
