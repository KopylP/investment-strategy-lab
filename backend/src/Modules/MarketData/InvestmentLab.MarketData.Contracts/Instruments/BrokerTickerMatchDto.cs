namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record BrokerTickerMatchDto(InstrumentDto? Instrument, string SuggestedSymbol);
