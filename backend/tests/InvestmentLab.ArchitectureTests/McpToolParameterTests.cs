using System.Reflection;
using ModelContextProtocol.Server;

namespace InvestmentLab.ArchitectureTests;

public sealed class McpToolParameterTests
{
    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    public void Every_nullable_tool_parameter_is_optional()
    {
        var requiredNullables = ToolMethods()
            .SelectMany(method => method.GetParameters())
            .Where(IsNullable)
            .Where(parameter => !parameter.HasDefaultValue)
            .Select(parameter => $"{parameter.Member.DeclaringType?.Name}.{parameter.Member.Name}({parameter.Name})");

        requiredNullables.ShouldBeEmpty();
    }

    private static IEnumerable<MethodInfo> ToolMethods() =>
        CodeLocation.Modules
            .SelectMany(module => Assembly.Load($"InvestmentLab.{module}").GetTypes())
            .Where(type => type.IsDefined(typeof(McpServerToolTypeAttribute), inherit: false))
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute), inherit: false));

    private static bool IsNullable(ParameterInfo parameter) =>
        Nullable.GetUnderlyingType(parameter.ParameterType) is not null
        || (!parameter.ParameterType.IsValueType && Nullability.Create(parameter).ReadState == NullabilityState.Nullable);
}
