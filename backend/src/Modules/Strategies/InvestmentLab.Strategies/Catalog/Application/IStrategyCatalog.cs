using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Catalog.Application;

/// <summary>Strategy types known from the engines, synced on demand and when stale.</summary>
public interface IStrategyCatalog
{
    Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Asks every engine for its types now; returns how many are known afterwards.</summary>
    Task<int> SyncAsync(CancellationToken cancellationToken);
}

public sealed record StrategyTypeDto(
    string Code,
    string Name,
    string Description,
    string Engine,
    string EngineVersion,
    bool SupportsState,
    IReadOnlyList<string> RequiredSeriesRoles,
    JsonElement ParametersSchema,
    JsonElement DefaultParameters,
    IReadOnlyList<ReportDefinition> Reports);

public sealed class GetStrategyCatalogHandler(IStrategyCatalog catalog)
{
    public async Task<IReadOnlyList<StrategyTypeDto>> Handle(CancellationToken cancellationToken) =>
        [.. (await catalog.ListAsync(cancellationToken)).Select(type => new StrategyTypeDto(
            type.Code, type.Name, type.Description, type.Engine, type.EngineVersion, type.SupportsState, type.RequiredSeriesRoles,
            JsonDocument.Parse(type.ParametersSchema).RootElement, JsonDocument.Parse(type.DefaultParameters).RootElement, type.Reports))];
}
