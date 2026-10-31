namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record NewInstrumentDto(string YahooSymbol, string Name, string TradingCurrency, string Kind);
