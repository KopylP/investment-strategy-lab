using InvestmentLab.Backtesting.Replays.Domain;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Backtesting.Replays.Application.Integration;

internal sealed class RequestBacktestsAgainOnLongerHistory(IBacktestRepository backtests, TimeProvider clock) : IIntegrationEventHandler<QuoteHistoryExtendedV1>
{
    public async Task Handle(QuoteHistoryExtendedV1 extended, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();

        foreach (var backtest in await backtests.AllAsync(cancellationToken))
            backtest.RequestAgain(now);

        await backtests.SaveChangesAsync(cancellationToken);
    }
}
