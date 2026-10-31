using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Application.Shared;

namespace InvestmentLab.Strategies.Catalog.Application.GetStrategyCatalog;

public sealed class GetStrategyCatalogHandler(IStrategyCatalog catalog)
{
    public async Task<IReadOnlyList<StrategyTypeDto>> Handle(CancellationToken cancellationToken) =>
        [.. (await catalog.ListAsync(cancellationToken)).Select(type => new StrategyTypeDto(
            type.Code, type.Name, type.Description, type.Engine, type.EngineVersion, type.SupportsState, type.RequiredSeriesRoles,
            JsonDocument.Parse(type.ParametersSchema).RootElement, JsonDocument.Parse(type.DefaultParameters).RootElement, type.Reports))];
}
