using System.Reflection;
using System.Text.Json;
using InvestmentLab.Strategies.Definitions.Application;
using InvestmentLab.Strategies.Shared.Infrastructure.Engine;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class DefaultStrategies : IDefaultStrategies
{
    private const string Resource = "InvestmentLab.Strategies.Definitions.Infrastructure.Seed.default-strategies.json";

    public IReadOnlyList<DefaultStrategy> Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource)
            ?? throw new InvalidOperationException($"Embedded resource {Resource} is missing.");

        return JsonSerializer.Deserialize<List<DefaultStrategy>>(stream, StrategyEngineClient.Json)!;
    }
}
