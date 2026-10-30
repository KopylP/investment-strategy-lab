using Architecture = ArchUnitNET.Domain.Architecture;
using Assembly = System.Reflection.Assembly;
using ArchUnitNET.Loader;

namespace InvestmentLab.ArchitectureTests;

public sealed class SolutionArchitectureTests
{
    private static readonly Assembly[] OwnAssemblies =
    [
        Assembly.Load("InvestmentLab.SharedKernel"),
        Assembly.Load("InvestmentLab.BuildingBlocks"),
        .. CodeLocation.Modules.SelectMany(module => new[]
        {
            Assembly.Load($"InvestmentLab.{module}"),
            Assembly.Load($"InvestmentLab.{module}.Contracts"),
        }),
    ];

    private static readonly Lazy<Architecture> Architecture = new(() => new ArchLoader().LoadAssemblies(OwnAssemblies).Build());

    [Fact]
    public void Every_type_dependency_follows_module_and_layer_rules()
    {
        var ownNames = OwnAssemblies.Select(assembly => assembly.GetName().Name).ToHashSet();

        var violations = Architecture.Value.Types
            .Where(type => ownNames.Contains(type.Assembly.Name.Split(',')[0]))
            .SelectMany(type => type.Dependencies.Select(dependency => (Type: type, dependency.Target)))
            // ArchUnitNET merges same-named method type parameters (Publish<TEvent>) across types into a false dependency.
            .Where(pair => pair.Target is not ArchUnitNET.Domain.GenericParameter)
            .Select(pair => (pair.Type, pair.Target, Reason: DependencyRules.Check(pair.Type.Namespace.FullName, pair.Target.Namespace.FullName)))
            .Where(result => result.Reason is not null)
            .Select(result => $"{result.Type.FullName} -> {result.Target.FullName}: {result.Reason}")
            .Distinct()
            .ToList();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Module_assemblies_reference_other_modules_only_through_contracts()
    {
        foreach (var module in CodeLocation.Modules)
        {
            var references = OwnReferences(Assembly.Load($"InvestmentLab.{module}"));

            references.Where(name => name is not ("InvestmentLab.SharedKernel" or "InvestmentLab.BuildingBlocks") && !name.EndsWith(".Contracts"))
                .ShouldBeEmpty($"InvestmentLab.{module} references module internals");
        }
    }

    [Fact]
    public void Contracts_assemblies_reference_no_other_investment_lab_assembly()
    {
        foreach (var module in CodeLocation.Modules)
            OwnReferences(Assembly.Load($"InvestmentLab.{module}.Contracts")).ShouldBeEmpty();
    }

    [Fact]
    public void Shared_kernel_references_no_other_investment_lab_assembly()
    {
        OwnReferences(Assembly.Load("InvestmentLab.SharedKernel")).ShouldBeEmpty();
    }

    private static List<string> OwnReferences(Assembly assembly) =>
        [.. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("InvestmentLab.", StringComparison.Ordinal))];
}
