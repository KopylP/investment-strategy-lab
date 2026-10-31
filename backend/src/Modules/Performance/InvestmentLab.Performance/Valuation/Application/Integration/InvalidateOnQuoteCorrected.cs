using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class InvalidateOnQuoteCorrected(SnapshotInvalidation snapshots) : IIntegrationEventHandler<QuoteCorrectedV1>
{
    public Task Handle(QuoteCorrectedV1 corrected, CancellationToken cancellationToken) =>
        snapshots.MarkAllAccountsAsync(corrected.TradingDate, cancellationToken);
}
