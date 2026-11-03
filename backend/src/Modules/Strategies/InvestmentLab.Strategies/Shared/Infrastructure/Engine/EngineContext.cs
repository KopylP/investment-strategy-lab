using System.Text.Json;
using System.Text.Json.Serialization;
using InvestmentLab.Strategies.Evaluation.Application.Shared;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineContext(
    DateOnly AsOf,
    JsonElement Parameters,
    IReadOnlyList<EngineUniverseMember> Universe,
    string? CashInstrumentKey,
    IReadOnlyList<EngineSeries> MarketSeries,
    EngineSubPortfolio SubPortfolio,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] IReadOnlyList<EngineSignalState>? Signals = null);
