using System.Reflection;
using System.Text.Json;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.ArchitectureTests;

public sealed class SignalKindContractTests
{
    [Fact]
    public void The_engine_and_the_backend_know_the_same_kinds_of_built_in_signals()
    {
        var inContract = KindsInEngineContract();
        var inBackend = KindsInBackend();

        inContract.ShouldBe(inBackend, ignoreOrder: true);
    }

    private static List<string> KindsInEngineContract()
    {
        var path = Path.Combine(RepositoryFolder.Backend().Parent!.FullName, "strategy-engine", "contracts", "v1", "openapi.json");
        using var contract = JsonDocument.Parse(File.ReadAllText(path));

        var kinds = contract.RootElement
            .GetProperty("components")
            .GetProperty("schemas")
            .GetProperty("SignalKind")
            .GetProperty("enum");

        return [.. kinds.EnumerateArray().Select(kind => kind.GetString()!)];
    }

    private static List<string> KindsInBackend()
    {
        var constants = typeof(BuiltInSignalKind).GetFields(BindingFlags.Public | BindingFlags.Static);

        return [.. constants.Select(constant => (string)constant.GetRawConstantValue()!)];
    }
}
