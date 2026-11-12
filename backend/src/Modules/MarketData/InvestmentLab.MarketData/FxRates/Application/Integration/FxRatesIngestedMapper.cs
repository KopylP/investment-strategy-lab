using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.FxRates;
using InvestmentLab.MarketData.FxRates.Domain;

namespace InvestmentLab.MarketData.FxRates.Application.Integration;

internal sealed class FxRatesIngestedMapper : IDomainEventMapper<FxRatesIngested, FxRatesIngestedV1>
{
    public FxRatesIngestedV1 Map(FxRatesIngested ingested) => new(ingested.OccurredAt, ingested.FirstEffectiveDate);
}
