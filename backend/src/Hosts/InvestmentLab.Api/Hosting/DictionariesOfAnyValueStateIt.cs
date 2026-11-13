using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentLab.Api.Hosting;

internal sealed class DictionariesOfAnyValueStateIt : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        if (IsDictionaryWithoutValueSchema(schema, context.JsonTypeInfo))
            schema.AdditionalProperties = new OpenApiSchema();

        return Task.CompletedTask;
    }

    private static bool IsDictionaryWithoutValueSchema(OpenApiSchema schema, JsonTypeInfo typeInfo) =>
        typeInfo.Kind == JsonTypeInfoKind.Dictionary && schema.AdditionalProperties is null;
}
