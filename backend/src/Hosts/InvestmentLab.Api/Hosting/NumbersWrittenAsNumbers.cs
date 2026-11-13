using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentLab.Api.Hosting;

internal sealed class NumbersWrittenAsNumbers : IOpenApiSchemaTransformer
{
    private const JsonSchemaType Numeric = JsonSchemaType.Number | JsonSchemaType.Integer;

    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (IsNumberAlsoReadFromText(schema.Type))
        {
            schema.Type &= ~JsonSchemaType.String;
            schema.Pattern = null;
        }

        return Task.CompletedTask;
    }

    private static bool IsNumberAlsoReadFromText(JsonSchemaType? type) =>
        type is { } known && (known & Numeric) != 0 && known.HasFlag(JsonSchemaType.String);
}
