using System.Collections.Concurrent;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;

namespace InvestmentLab.Api.Hosting;

internal sealed class OneSchemaPerName
{
    private readonly ConcurrentDictionary<string, Type> _typesByName = new();

    public string? NameOf(JsonTypeInfo typeInfo)
    {
        var name = OpenApiOptions.CreateDefaultSchemaReferenceId(typeInfo);
        if (name is null)
            return null;

        var type = WithoutNullable(typeInfo.Type);
        var owner = _typesByName.GetOrAdd(name, type);
        if (owner != type)
            throw new InvalidOperationException(
                $"The OpenAPI schema name '{name}' belongs to both {owner.FullName} and {type.FullName}; rename one of them.");

        return name;
    }

    private static Type WithoutNullable(Type type) => Nullable.GetUnderlyingType(type) ?? type;
}
