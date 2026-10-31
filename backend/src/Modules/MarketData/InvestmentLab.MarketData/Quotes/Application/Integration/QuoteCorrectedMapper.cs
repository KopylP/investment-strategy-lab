using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.MarketData.Contracts.Quotes;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.Integration;

internal sealed class QuoteCorrectedMapper : IDomainEventMapper<QuoteCorrected, QuoteCorrectedV1>
{
    public QuoteCorrectedV1 Map(QuoteCorrected corrected) =>
        new(corrected.InstrumentId.Value, corrected.TradingDate);
}
