using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentLab.Api.Hosting;

internal sealed class ComputedPropertiesKeepTheirNullability : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (schema.Properties is null)
            return Task.CompletedTask;

        var neverNull = context.JsonTypeInfo.Properties.Where(IsComputedAndNeverNull);

        foreach (var property in neverNull)
        {
            if (schema.Properties.TryGetValue(property.Name, out var described) && described is OpenApiSchema written)
                written.Type &= ~JsonSchemaType.Null;
        }

        return Task.CompletedTask;
    }

    private static bool IsComputedAndNeverNull(JsonPropertyInfo property) =>
        property is { Get: not null, Set: null, AssociatedParameter: null, IsGetNullable: false }
        && !property.PropertyType.IsValueType;
}
