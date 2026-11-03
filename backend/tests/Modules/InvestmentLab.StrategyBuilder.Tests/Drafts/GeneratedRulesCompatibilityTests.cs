using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Json.Schema;

namespace InvestmentLab.StrategyBuilder.Tests.Drafts;

public sealed class GeneratedRulesCompatibilityTests
{
    private static readonly Lazy<JsonSchema> Schema = new(() => JsonSchema.FromText(File.ReadAllText(EngineSchema.SchemaPath), new BuildOptions { Dialect = Dialect.Draft202012 }));

    public static TheoryData<string> Names => [.. CompatibilityDrafts.All().Keys.Order()];

    private static string FixturePath(string name) =>
        Path.Combine(EngineSchema.Backend().FullName, "tests", "Modules", "InvestmentLab.StrategyBuilder.Tests", "Fixtures", "GeneratedRules", $"{name}.json");

    private static string Generated(string name) => RulesGenerator.Generate(CompatibilityDrafts.All()[name]).Json;

    [Theory]
    [MemberData(nameof(Names))]
    public void Every_compatibility_draft_passes_the_local_check(string name) =>
        GraphCheck.Of(CompatibilityDrafts.All()[name], EngineSchema.Vocabulary).ShouldBeEmpty();

    [Theory]
    [MemberData(nameof(Names))]
    public void Generated_rules_fit_the_engine_schema(string name)
    {
        var evaluation = Schema.Value.Evaluate(JsonDocument.Parse(Generated(name)).RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });

        evaluation.IsValid.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(Names))]
    public void Generated_rules_match_their_fixture(string name)
    {
        var path = FixturePath(name);
        if (Environment.GetEnvironmentVariable("UPDATE_FIXTURES") == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, Generated(name) + "\n");
        }

        File.ReadAllText(path).TrimEnd().ShouldBe(Generated(name));
    }
}
