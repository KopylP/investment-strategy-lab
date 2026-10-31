using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Application;

public sealed record QuotedInstrument(InstrumentId Id, ProviderSymbol Symbol, Currency TradingCurrency);
