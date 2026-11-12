using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.FxRates;

namespace InvestmentLab.Performance.Valuation.Application.Integration;

internal sealed class InvalidateOnFxRatesIngested(SnapshotInvalidation snapshots) : IIntegrationEventHandler<FxRatesIngestedV1>
{
    public Task Handle(FxRatesIngestedV1 ingested, CancellationToken cancellationToken) =>
        snapshots.MarkAllAccountsAsync(ingested.FirstEffectiveDate, cancellationToken);
}
