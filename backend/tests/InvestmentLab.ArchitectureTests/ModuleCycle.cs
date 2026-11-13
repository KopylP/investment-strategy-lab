namespace InvestmentLab.ArchitectureTests;

public sealed class ModuleCycle
{
    public ModuleCycle(IReadOnlyList<string> modules)
    {
        var first = modules.Order(StringComparer.Ordinal).First();
        var firstAt = modules.ToList().IndexOf(first);

        Modules = [.. modules.Skip(firstAt), .. modules.Take(firstAt)];
    }

    public IReadOnlyList<string> Modules { get; }

    public override string ToString() => string.Join(" -> ", Modules.Append(Modules[0]));
}
