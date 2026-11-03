using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Application.Shared;
using InvestmentLab.Strategies.Catalog.Domain;

namespace InvestmentLab.Strategies.Catalog.Application.GetStrategyCatalog;

public sealed class GetStrategyCatalogHandler(IStrategyCatalog catalog)
{
    public async Task<IReadOnlyList<StrategyTypeDto>> Handle(CancellationToken cancellationToken)
    {
        var types = await catalog.ListAsync(cancellationToken);

        return [.. types.Select(ToDto)];
    }

    private static StrategyTypeDto ToDto(StrategyTypeDescriptor type) =>
        new(
            type.Code,
            type.Name,
            type.Description,
            type.Engine,
            type.EngineVersion,
            type.SupportsState,
            type.RequiredSeriesRoles,
            JsonDocument.Parse(type.ParametersSchema).RootElement,
            type.DefaultParameters is null ? null : JsonDocument.Parse(type.DefaultParameters).RootElement,
            type.Reports);
}
