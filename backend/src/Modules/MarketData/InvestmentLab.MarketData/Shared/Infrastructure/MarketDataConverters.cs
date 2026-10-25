using InvestmentLab.MarketData.Instruments.Domain;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.MarketData.Shared.Infrastructure;

internal static class MarketDataConverters
{
    public sealed class InstrumentIdConverter() : ValueConverter<InstrumentId, Guid>(id => id.Value, value => InstrumentId.Of(value));
}
