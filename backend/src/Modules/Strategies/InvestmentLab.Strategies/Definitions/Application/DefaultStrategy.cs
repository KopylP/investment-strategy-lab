using System.Text.Json;

namespace InvestmentLab.Strategies.Definitions.Application;

public sealed record DefaultStrategy(
    string Name,
    string TypeCode,
    string Currency,
    IReadOnlyList<string> Universe,
    string? CashInstrument,
    string Benchmark,
    string? VolatilityIndex,
    JsonElement Parameters);
