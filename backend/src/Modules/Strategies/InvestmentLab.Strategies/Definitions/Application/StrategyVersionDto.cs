using System.Text.Json;
using Json.Schema;

namespace InvestmentLab.Strategies.Definitions.Application;

public sealed record StrategyVersionDto(
    int Number,
    JsonElement Parameters,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    Guid BenchmarkInstrumentId,
    Guid? VolatilityIndexInstrumentId,
    DateTimeOffset CreatedAt);
