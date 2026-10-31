using System.Text.Json.Serialization;

namespace InvestmentLab.Portfolio.Transactions.Api;

public sealed record PositionsFile(
    [property: JsonPropertyName("cash")] decimal Cash,
    [property: JsonPropertyName("positions")] IReadOnlyList<PositionsFileEntry> Positions);
