namespace InvestmentLab.ArchitectureTests;

public sealed class ModuleCycleTests
{
    private static readonly ModuleCycle[] KnownCycles =
    [
        new(["Portfolio", "Taxation"]),
        new(["Portfolio", "Strategies"]),
        new(["Portfolio", "Strategies", "Signals"]),
    ];

    private static readonly Lazy<List<string>> FoundCycles =
        new(() => [.. ModuleDependencyGraph.OfCompiledModules().Cycles().Select(cycle => cycle.ToString())]);

    [Fact]
    public void Modules_use_each_others_contracts_in_no_cycle_beyond_the_known_ones()
    {
        var known = KnownCycles.Select(cycle => cycle.ToString());

        var newCycles = FoundCycles.Value.Except(known).ToList();

        newCycles.ShouldBeEmpty(string.Join(Environment.NewLine, newCycles));
    }

    [Fact]
    public void Every_known_cycle_still_exists_so_a_broken_one_is_removed_from_the_list()
    {
        var known = KnownCycles.Select(cycle => cycle.ToString());

        var brokenCycles = known.Except(FoundCycles.Value).ToList();

        brokenCycles.ShouldBeEmpty(string.Join(Environment.NewLine, brokenCycles));
    }
}
