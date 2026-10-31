using System.Text.Json.Serialization;

namespace InvestmentLab.Portfolio.Transactions.Api;

public sealed record PositionsFileEntry(
    [property: JsonPropertyName("ticker")] string Ticker,
    [property: JsonPropertyName("entry_price")] decimal EntryPrice,
    [property: JsonPropertyName("shares")] decimal Shares,
    [property: JsonPropertyName("entry_date")] DateOnly? EntryDate,
    [property: JsonPropertyName("currency")] string Currency);
