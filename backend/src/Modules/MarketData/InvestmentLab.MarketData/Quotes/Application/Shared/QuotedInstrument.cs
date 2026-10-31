using InvestmentLab.MarketData.Instruments.Domain;

namespace InvestmentLab.MarketData.Quotes.Application.Shared;

public sealed record QuotedInstrument(InstrumentId Id, ProviderSymbol Symbol, Currency TradingCurrency);
