using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Instruments.Application.Shared;

internal static class InstrumentMapping
{
    extension(Instrument instrument)
    {
        public InstrumentDto ToDto() =>
            new(instrument.Id.Value, instrument.Kind.ToString(), instrument.Isin?.Value, instrument.Name, instrument.Exchange,
                instrument.TradingCurrency.Code, instrument.Group?.Name, instrument.IsAccumulating,
                [.. instrument.ProviderSymbols.Select(symbol => symbol.ToString())], instrument.Description, instrument.Ticker, instrument.IsVolatilityIndex,
                instrument.AssetClass?.ToString(), instrument.Region?.ToString(), instrument.AssetCurrency?.Code);
    }
}
