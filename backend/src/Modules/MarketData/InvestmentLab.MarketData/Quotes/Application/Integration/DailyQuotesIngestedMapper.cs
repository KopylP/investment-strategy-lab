using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.Integration;

internal sealed class DailyQuotesIngestedMapper : IDomainEventMapper<DailyQuotesIngested, DailyQuotesIngestedV1>
{
    public DailyQuotesIngestedV1 Map(DailyQuotesIngested ingested) =>
        new(ingested.OccurredAt, ingested.InstrumentIds, ingested.FirstTradingDate);
}
