using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InvestmentLab.Api.Hosting;

internal sealed class AnswersCarryEveryProperty : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        var schemas = document.Components?.Schemas;
        if (schemas is null)
            return Task.CompletedTask;

        var sent = new HashSet<string>();
        foreach (var schema in SchemasSentByClients(document))
            CollectReferences(schema, schemas, sent);

        foreach (var (name, schema) in schemas)
        {
            if (!sent.Contains(name) && schema is OpenApiSchema answer)
                RequireEveryProperty(answer);
        }

        return Task.CompletedTask;
    }

    private static IEnumerable<IOpenApiSchema> SchemasSentByClients(OpenApiDocument document)
    {
        var operations = document.Paths.Values
            .Where(path => path.Operations is not null)
            .SelectMany(path => path.Operations!.Values);

        foreach (var operation in operations)
        {
            var bodies = operation.RequestBody?.Content?.Values ?? [];
            var parameters = operation.Parameters ?? [];

            foreach (var body in bodies.Where(body => body.Schema is not null))
                yield return body.Schema!;

            foreach (var parameter in parameters.Where(parameter => parameter.Schema is not null))
                yield return parameter.Schema!;
        }
    }

    private static void CollectReferences(
        IOpenApiSchema schema,
        IDictionary<string, IOpenApiSchema> schemas,
        HashSet<string> found)
    {
        if (schema is OpenApiSchemaReference reference)
        {
            var name = reference.Reference.Id;
            if (name is not null && found.Add(name) && schemas.TryGetValue(name, out var target))
                CollectReferences(target, schemas, found);

            return;
        }

        foreach (var part in PartsOf(schema))
            CollectReferences(part, schemas, found);
    }

    private static IEnumerable<IOpenApiSchema> PartsOf(IOpenApiSchema schema)
    {
        IEnumerable<IOpenApiSchema?> parts =
        [
            .. schema.Properties?.Values ?? [],
            .. schema.AllOf ?? [],
            .. schema.OneOf ?? [],
            .. schema.AnyOf ?? [],
            schema.Items,
            schema.AdditionalProperties,
        ];

        return parts.OfType<IOpenApiSchema>();
    }

    private static void RequireEveryProperty(OpenApiSchema schema)
    {
        if (schema.Properties is not { Count: > 0 } properties)
            return;

        schema.Required ??= new HashSet<string>();
        foreach (var property in properties.Keys)
            schema.Required.Add(property);
    }
}
