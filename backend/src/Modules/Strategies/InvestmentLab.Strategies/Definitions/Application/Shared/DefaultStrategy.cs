using System.Text.Json;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public sealed record DefaultStrategy(
    string Name,
    string TypeCode,
    string Currency,
    IReadOnlyList<string> Universe,
    string? CashInstrument,
    string Benchmark,
    bool UsesVolatilityIndex,
    JsonElement Parameters);
