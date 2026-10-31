using System.Text.Json;
using Json.Schema;

namespace InvestmentLab.Strategies.Definitions.Application;

public sealed record StrategySetupInput(
    IReadOnlyList<Guid> Universe,
    Guid BenchmarkInstrumentId,
    Guid? VolatilityIndexInstrumentId = null,
    Guid? CashInstrumentId = null,
    JsonElement? Parameters = null);
