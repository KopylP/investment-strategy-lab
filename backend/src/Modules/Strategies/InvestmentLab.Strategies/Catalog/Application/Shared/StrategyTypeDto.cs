using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Catalog.Application.Shared;

public sealed record StrategyTypeDto(
    string Code,
    string Name,
    string Description,
    string Engine,
    string EngineVersion,
    bool SupportsState,
    IReadOnlyList<string> RequiredSeriesRoles,
    JsonElement ParametersSchema,
    JsonElement? DefaultParameters,
    IReadOnlyList<ReportDefinition> Reports);
