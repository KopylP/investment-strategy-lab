namespace InvestmentLab.MarketData.Contracts.Instruments;

public sealed record InstrumentDto(
    Guid Id,
    string Kind,
    string? Isin,
    string Name,
    string Exchange,
    string TradingCurrency,
    string? Group,
    bool? IsAccumulating,
    IReadOnlyList<string> ProviderSymbols,
    string? Description = null,
    string? Ticker = null,
    bool IsVolatilityIndex = false)
{
    public string Label => Ticker ?? Name;
}
