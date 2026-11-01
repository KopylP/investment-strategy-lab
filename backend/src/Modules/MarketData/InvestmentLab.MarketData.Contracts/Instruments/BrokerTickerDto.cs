namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record BrokerTickerDto(string Ticker, string Currency, bool IsExactSymbol = false);
