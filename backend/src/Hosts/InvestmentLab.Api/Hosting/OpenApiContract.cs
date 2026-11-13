using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.Api.Hosting;

public static class OpenApiContract
{
    private const string BuildTimeGenerator = "GetDocument.Insider";

    private static bool IsGeneratingAtBuild => Assembly.GetEntryAssembly()?.GetName().Name == BuildTimeGenerator;

    public static void AddOpenApiContract(this WebApplicationBuilder builder) =>
        builder.Services.AddOpenApi(options =>
        {
            options.CreateSchemaReferenceId = new OneSchemaPerName().NameOf;
            options.AddSchemaTransformer<NumbersWrittenAsNumbers>();
            options.AddSchemaTransformer<ComputedPropertiesKeepTheirNullability>();
            options.AddDocumentTransformer<AnswersCarryEveryProperty>();
        });

    public static void LeaveOutBackgroundWorkWhenGeneratingAtBuild(this WebApplicationBuilder builder)
    {
        if (IsGeneratingAtBuild)
            builder.Services.RemoveAll<IHostedService>();
    }
}
