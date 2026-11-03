using InvestmentLab.StrategyBuilder.Drafts.Domain;
using InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

namespace InvestmentLab.StrategyBuilder.Tests;

internal static class EngineSchema
{
    private const string SolutionFile = "InvestmentLab.slnx";

    private static readonly Lazy<RuleVocabulary> Read = new(() => RuleVocabularyFromSchema.Read(File.ReadAllText(SchemaPath), []));

    public static string SchemaPath => Path.Combine(Repository().FullName, "strategy-engine", "contracts", "v1", "rule-based.parameters.schema.json");

    public static string CatalogPath => Path.Combine(Repository().FullName, "locales", "uk.json");

    public static RuleVocabulary Vocabulary => Read.Value;

    public static DirectoryInfo Backend()
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);

        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, SolutionFile)))
            folder = folder.Parent;

        return folder ?? throw new InvalidOperationException($"{SolutionFile} was not found above the test run.");
    }

    private static DirectoryInfo Repository() => Backend().Parent!;
}
