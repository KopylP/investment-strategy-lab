using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace InvestmentLab.ArchitectureTests;

public sealed partial class ForeignContractIsolationTests
{
    private const string IntegrationFolder = ".Application.Integration";

    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    [Fact]
    public void A_dto_of_another_module_stays_inside_the_class_that_reads_it()
    {
        var offenders = CodeLocation.Modules
            .SelectMany(OffendersIn)
            .Distinct()
            .Order()
            .ToList();

        offenders.ShouldBeEmpty(string.Join(Environment.NewLine, offenders));
    }

    [Fact]
    public void No_type_is_named_input_because_the_word_says_nothing_of_what_it_is()
    {
        var named = CodeLocation.Modules
            .SelectMany(TypesOf)
            .Where(type => InputName().IsMatch(type.Name))
            .Select(type => type.FullName!)
            .ToList();

        named.ShouldBeEmpty();
    }

    private static IEnumerable<string> OffendersIn(string module) =>
        TypesOf(module).Where(IsCheckedType).SelectMany(type => OffendingMembers(type, module));

    private static IEnumerable<Type> TypesOf(string module) =>
        Assembly.Load($"InvestmentLab.{module}").GetTypes();

    private static bool IsCheckedType(Type type) =>
        !type.IsNestedPrivate && !IsInsidePrivateType(type) && !IsCompilerGenerated(type);

    private static bool IsInsidePrivateType(Type type) =>
        type.DeclaringType is { } outer && (outer.IsNestedPrivate || IsInsidePrivateType(outer) || IsCompilerGenerated(outer));

    private static bool IsCompilerGenerated(MemberInfo member) =>
        member.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

    private static IEnumerable<string> OffendingMembers(Type type, string module)
    {
        var exposed = MethodTypes(type)
            .Concat(ConstructorTypes(type))
            .Concat(FieldTypes(type))
            .Concat(InheritedTypes(type));

        return exposed
            .SelectMany(TypesWithin)
            .Where(seen => IsDtoOfAnotherModule(seen, module))
            .Where(seen => !IsEventInItsIntegrationFolder(seen, type))
            .Select(seen => $"{type.FullName} exposes {seen.FullName}");
    }

    private static IEnumerable<Type> MethodTypes(Type type) =>
        type.GetMethods(AllDeclared)
            .Where(method => !method.IsPrivate)
            .SelectMany(method => method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType));

    private static IEnumerable<Type> ConstructorTypes(Type type) =>
        type.GetConstructors(AllDeclared)
            .Where(constructor => !constructor.IsPrivate)
            .SelectMany(constructor => constructor.GetParameters().Select(parameter => parameter.ParameterType));

    private static IEnumerable<Type> FieldTypes(Type type) =>
        type.GetFields(AllDeclared)
            .Where(field => !field.IsPrivate && !IsCompilerGenerated(field))
            .Select(field => field.FieldType);

    private static IEnumerable<Type> InheritedTypes(Type type)
    {
        IEnumerable<Type> baseType = type.BaseType is { } parent ? [parent] : [];

        return baseType.Concat(type.GetInterfaces());
    }

    private static bool IsEventInItsIntegrationFolder(Type seen, Type user) =>
        VersionedEvent().IsMatch(seen.Name) && user.Namespace!.Contains(IntegrationFolder, StringComparison.Ordinal);

    private static IEnumerable<Type> TypesWithin(Type type)
    {
        var element = type.GetElementType();
        var within = type.GetGenericArguments().Concat(element is null ? [] : [element]);

        return within.SelectMany(TypesWithin).Append(type);
    }

    private static bool IsDtoOfAnotherModule(Type type, string module)
    {
        if (type.IsInterface || type.IsGenericParameter || type.Namespace is null)
            return false;

        var location = CodeLocation.Parse(type.Namespace);

        return location.Kind == LocationKind.Contracts && location.Module != module;
    }

    [GeneratedRegex("Inputs?$")]
    private static partial Regex InputName();

    [GeneratedRegex(@"V\d+$")]
    private static partial Regex VersionedEvent();
}
