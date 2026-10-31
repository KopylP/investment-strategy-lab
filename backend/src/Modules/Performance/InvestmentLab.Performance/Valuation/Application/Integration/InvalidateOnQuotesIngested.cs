using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class InvalidateOnQuotesIngested(SnapshotInvalidation snapshots) : IIntegrationEventHandler<DailyQuotesIngestedV1>
{
    public Task Handle(DailyQuotesIngestedV1 ingested, CancellationToken cancellationToken) =>
        snapshots.MarkAllAccountsAsync(ingested.FirstTradingDate, cancellationToken);
}
