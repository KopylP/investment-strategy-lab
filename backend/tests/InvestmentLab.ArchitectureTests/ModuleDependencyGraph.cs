using Assembly = System.Reflection.Assembly;

namespace InvestmentLab.ArchitectureTests;

public sealed class ModuleDependencyGraph
{
    private const string ContractsSuffix = ".Contracts";
    private const string OwnPrefix = "InvestmentLab.";

    private readonly Dictionary<string, List<string>> _usedModules;

    private ModuleDependencyGraph(Dictionary<string, List<string>> usedModules) => _usedModules = usedModules;

    public static ModuleDependencyGraph OfCompiledModules()
    {
        var usedModules = CodeLocation.Modules.ToDictionary(module => module, ModulesWhoseContractsAreUsedBy);

        return new ModuleDependencyGraph(usedModules);
    }

    public IReadOnlyList<ModuleCycle> Cycles()
    {
        var cycles = new List<ModuleCycle>();

        foreach (var start in _usedModules.Keys.Order(StringComparer.Ordinal))
            FollowPathsBackTo(start, [start], cycles);

        return cycles;
    }

    private static List<string> ModulesWhoseContractsAreUsedBy(string module)
    {
        var references = Assembly.Load($"{OwnPrefix}{module}").GetReferencedAssemblies().Select(reference => reference.Name!);

        return [.. references
            .Where(name => name.StartsWith(OwnPrefix, StringComparison.Ordinal) && name.EndsWith(ContractsSuffix, StringComparison.Ordinal))
            .Select(name => name[OwnPrefix.Length..^ContractsSuffix.Length])
            .Where(used => used != module)];
    }

    private void FollowPathsBackTo(string start, List<string> path, List<ModuleCycle> cycles)
    {
        foreach (var next in _usedModules[path[^1]])
        {
            if (next == start)
                cycles.Add(new ModuleCycle([.. path]));
            else if (CanExtendPath(start, next, path))
                FollowPathsBackTo(start, [.. path, next], cycles);
        }
    }

    private static bool CanExtendPath(string start, string next, List<string> path) =>
        string.CompareOrdinal(next, start) > 0 && !path.Contains(next);
}
