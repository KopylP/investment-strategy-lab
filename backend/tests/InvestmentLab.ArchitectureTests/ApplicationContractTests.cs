using System.Reflection;

namespace InvestmentLab.ArchitectureTests;

public sealed class ApplicationContractTests
{
    private const string HandleMethod = "Handle";

    private const BindingFlags PublicDeclared =
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void A_handler_takes_and_returns_only_its_own_records_and_never_a_domain_type()
    {
        var offenders = CodeLocation.Modules
            .SelectMany(TypesOf)
            .Where(IsHandler)
            .SelectMany(OffendersIn)
            .Distinct()
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void Endpoints_and_tools_take_and_return_no_domain_type()
    {
        var offenders = CodeLocation.Modules
            .SelectMany(TypesOf)
            .Where(type => IsInLayer(type, "Api"))
            .SelectMany(OffendersIn)
            .Distinct()
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(string.Join(Environment.NewLine, offenders));
    }

    private static IEnumerable<Type> TypesOf(string module) =>
        Assembly.Load($"InvestmentLab.{module}").GetTypes().Where(type => !IsCompilerGenerated(type));

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false);

    private static bool IsHandler(Type type) =>
        type.IsClass && IsInLayer(type, "Application") && type.GetMethod(HandleMethod, PublicDeclared) is not null;

    private static IEnumerable<string> OffendersIn(Type entry)
    {
        var visited = new HashSet<Type>();

        return SignatureTypes(entry)
            .Concat(PropertyTypes(entry))
            .SelectMany(TypesWithin)
            .SelectMany(type => DomainTypesReachedFrom(type, visited, path: entry.Name))
            .Select(found => $"{found.Path} exposes {found.Type.FullName}");
    }

    private static IEnumerable<Type> SignatureTypes(Type entry) =>
        entry.GetMethods(PublicDeclared)
            .Where(method => !method.IsSpecialName)
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType));

    private static IEnumerable<Type> PropertyTypes(Type entry) =>
        entry.GetProperties(PublicDeclared).Select(property => property.PropertyType);

    private static IEnumerable<(string Path, Type Type)> DomainTypesReachedFrom(Type type, HashSet<Type> visited, string path)
    {
        if (!visited.Add(type))
            return [];

        if (IsInLayer(type, "Domain"))
            return [(path, type)];

        if (!IsInLayer(type, "Application"))
            return [];

        return type.GetProperties(PublicDeclared)
            .SelectMany(property => TypesWithin(property.PropertyType)
                .SelectMany(within => DomainTypesReachedFrom(within, visited, $"{type.Name}.{property.Name}")));
    }

    private static IEnumerable<Type> TypesWithin(Type type)
    {
        var element = type.GetElementType();
        var within = type.GetGenericArguments().Concat(element is null ? [] : [element]);

        return within.SelectMany(TypesWithin).Append(type);
    }

    private static bool IsInLayer(Type type, string layer)
    {
        if (type.IsGenericParameter || type.Namespace is null)
            return false;

        var location = CodeLocation.Parse(type.Namespace);

        return location.Kind == LocationKind.Module && location.Layer == layer;
    }
}
