namespace InvestmentLab.ArchitectureTests;

internal static class DependencyRules
{
    private static readonly string[] Infrastructure = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql"];
    private static readonly string[] PersistenceAndHttp = ["Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Npgsql"];

    public static string? Check(string fromNamespace, string toNamespace)
    {
        var from = CodeLocation.Parse(fromNamespace);
        var to = CodeLocation.Parse(toNamespace);

        return from.Kind switch
        {
            LocationKind.SharedKernel => CheckSharedKernel(to),
            LocationKind.BuildingBlocks => to.Kind is LocationKind.Contracts or LocationKind.Module or LocationKind.Host
                ? "BuildingBlocks must not know modules or hosts"
                : null,
            LocationKind.Contracts => to.Kind == LocationKind.External || (to.Kind == LocationKind.Contracts && to.Module == from.Module)
                ? null
                : "Contracts carry primitives only and reference nothing else of InvestmentLab",
            LocationKind.Module => CheckModule(from, to),
            _ => null,
        };
    }

    private static string? CheckSharedKernel(CodeLocation to) => to.Kind switch
    {
        LocationKind.SharedKernel => null,
        LocationKind.External when to.IsUnder(Infrastructure) => "SharedKernel must not depend on infrastructure frameworks",
        LocationKind.External => null,
        _ => "SharedKernel must not depend on other InvestmentLab code",
    };

    private static string? CheckModule(CodeLocation from, CodeLocation to)
    {
        switch (to.Kind)
        {
            case LocationKind.SharedKernel:
                return null;
            case LocationKind.External:
                return CheckFramework(from, to);
            case LocationKind.BuildingBlocks:
                return from.Layer == "Domain" ? "Domain depends only on SharedKernel" : null;
            case LocationKind.Contracts:
                return from.Layer == "Domain" ? "Domain must not depend on Contracts" : null;
            case LocationKind.Host:
                return "Modules must not depend on hosts";
        }

        if (to.Module != from.Module)
            return $"Module {from.Module} may use {to.Module} only through InvestmentLab.{to.Module}.Contracts";

        if (from.IsModuleRoot)
            return null;

        if (from.IsSharedFeature)
            return to.IsModuleRoot || to.IsSharedFeature || to.Layer is "Domain" or "Infrastructure"
                ? null
                : "Shared holds persistence only and may reference feature Domain/Infrastructure";
        if (to.IsModuleRoot)
            return "Features must not depend on the module root (composition)";

        if (from.Layer is null)
            return $"Types of feature {from.Feature} must live in Domain, Application, Infrastructure or Api";

        if (to.IsSharedFeature)
            return from.Layer == "Infrastructure" ? null : "Only Infrastructure may use the module's Shared infrastructure";

        if (to.Feature == from.Feature)
            return CheckSameFeature(from.Layer, to.Layer);

        return to.Layer == "Domain" && from.Layer != "Api"
            ? null
            : $"Feature {from.Feature} may use feature {to.Feature} only through its Domain";
    }

    private static string? CheckSameFeature(string fromLayer, string? toLayer)
    {
        string[] allowed = fromLayer switch
        {
            "Domain" => ["Domain"],
            "Application" => ["Domain", "Application"],
            "Infrastructure" => ["Domain", "Application", "Infrastructure"],
            "Api" => ["Application", "Api"],
            _ => [],
        };

        return toLayer is not null && allowed.Contains(toLayer) ? null : $"{fromLayer} must not depend on {toLayer ?? "feature root"}";
    }

    private static string? CheckFramework(CodeLocation from, CodeLocation to) => from.Layer switch
    {
        "Domain" when to.IsUnder(Infrastructure) => "Domain must not depend on EF Core, ASP.NET Core or Npgsql",
        "Application" when to.IsUnder(PersistenceAndHttp) => "Application must not depend on EF Core, ASP.NET Core or Npgsql",
        _ => null,
    };
}
