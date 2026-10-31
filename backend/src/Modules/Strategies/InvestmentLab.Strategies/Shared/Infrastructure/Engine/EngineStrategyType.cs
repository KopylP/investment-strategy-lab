using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Shared.Infrastructure.Engine;

public sealed record EngineStrategyType(
    string Code,
    string Name,
    string Description,
    string Engine,
    string Version,
    bool SupportsState,
    IReadOnlyList<string> RequiredSeriesRoles,
    JsonElement ParametersSchema,
    JsonElement DefaultParameters,
    IReadOnlyList<ReportDefinition>? Reports = null);
