using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Application.Shared;

namespace InvestmentLab.Strategies.Tests.Definitions;

internal sealed class AlternativeParameterTypes : IStrategyTypes
{
    public async Task<IReadOnlyList<StrategyTypeDescriptor>> ListAsync(CancellationToken cancellationToken) =>
        [(await FindAsync("alternative", cancellationToken))!];

    public Task<StrategyTypeDescriptor?> FindAsync(string code, CancellationToken cancellationToken)
    {
        var type = StrategyTypeDescriptor.Of(
            code: "alternative",
            name: "Alternative parameters",
            description: "",
            engine: "test",
            engineVersion: "1",
            supportsState: false,
            requiredSeriesRoles: [],
            parametersSchema: """{"type":"object","properties":{"value":{"anyOf":[{"type":"number"},{"type":"null"}]}},"required":["value"]}""",
            defaultParameters: null,
            reports: [],
            syncedAt: DateTimeOffset.UnixEpoch);

        return Task.FromResult<StrategyTypeDescriptor?>(type);
    }
}
