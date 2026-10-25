namespace InvestmentLab.ArchitectureTests;

internal enum LocationKind
{
    SharedKernel,
    BuildingBlocks,
    Contracts,
    Module,
    Host,
    External,
}

/// <summary>Where a namespace sits in the architecture: InvestmentLab.&lt;Module&gt;.&lt;Feature&gt;.&lt;Layer&gt;.</summary>
internal sealed record CodeLocation(LocationKind Kind, string Namespace, string? Module = null, string? Feature = null, string? Layer = null)
{
    public const string SharedFeature = "Shared";

    public static readonly string[] Modules = ["Identity", "MarketData", "Strategies", "Portfolio", "Rebalancing", "Taxation", "Performance"];
    public static readonly string[] Layers = ["Domain", "Application", "Infrastructure", "Api"];
    private static readonly string[] Hosts = ["Api", "AppHost", "ServiceDefaults"];

    public bool IsModuleRoot => Kind == LocationKind.Module && Feature is null;
    public bool IsSharedFeature => Kind == LocationKind.Module && Feature == SharedFeature;

    public static CodeLocation Parse(string ns)
    {
        var parts = ns.Split('.');
        if (parts is not ["InvestmentLab", var second, ..])
            return new(LocationKind.External, ns);

        if (second == "SharedKernel")
            return new(LocationKind.SharedKernel, ns);
        if (second == "BuildingBlocks")
            return new(LocationKind.BuildingBlocks, ns);
        if (Hosts.Contains(second))
            return new(LocationKind.Host, ns);
        if (!Modules.Contains(second))
            return new(LocationKind.External, ns);

        if (parts.Length > 2 && parts[2] == "Contracts")
            return new(LocationKind.Contracts, ns, second);

        var feature = parts.Length > 2 ? parts[2] : null;
        var layer = parts.Length > 3 && Layers.Contains(parts[3]) ? parts[3] : null;
        return new(LocationKind.Module, ns, second, feature, layer);
    }

    public bool IsUnder(params string[] prefixes) =>
        prefixes.Any(prefix => Namespace == prefix || Namespace.StartsWith(prefix + ".", StringComparison.Ordinal));
}
