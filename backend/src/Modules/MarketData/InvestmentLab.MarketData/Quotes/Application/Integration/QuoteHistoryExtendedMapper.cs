using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.Integration;

internal sealed class QuoteHistoryExtendedMapper : IDomainEventMapper<QuoteHistoryExtended, QuoteHistoryExtendedV1>
{
    public QuoteHistoryExtendedV1 Map(QuoteHistoryExtended extended) => new(extended.OccurredAt);
}
