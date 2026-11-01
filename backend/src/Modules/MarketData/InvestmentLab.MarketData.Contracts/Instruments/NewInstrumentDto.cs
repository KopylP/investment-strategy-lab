namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record NewInstrumentDto(string Symbol, string Name, string TradingCurrency, string Kind);
