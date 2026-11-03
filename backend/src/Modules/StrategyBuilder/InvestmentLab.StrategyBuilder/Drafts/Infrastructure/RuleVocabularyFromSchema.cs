using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.StrategyBuilder.Drafts.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class RuleVocabularyFromSchema(IStrategyTypeQueries types) : IRuleVocabulary
{
    private static readonly string[] IndicatorFieldNames = ["indicator", "other", "rankBy"];
    private static readonly string[] BlockDefinitions = ["MarketFilter", "Selection", "Weights", "Risk"];
    private static readonly string[] LinkedFieldNames = ["kind", "left", "right", "conditions", "condition"];

    public async Task<Result<RuleVocabulary>> GetAsync(CancellationToken cancellationToken)
    {
        var type = await types.GetTypeAsync(RuleBasedType.Code, cancellationToken);
        if (type is null)
            return DraftErrors.VocabularyUnavailable();

        var reports = type.Reports.Select(DeclaredReportOf).ToList();

        return Read(type.ParametersSchema, reports);
    }

    public static RuleVocabulary Read(string parametersSchema, IReadOnlyList<DeclaredDraftReport> reports)
    {
        using var document = JsonDocument.Parse(parametersSchema);
        var root = document.RootElement;
        var definitions = root.GetProperty("$defs");

        var indicators = IndicatorDefinitions(root).Select(entry => IndicatorOf(entry.Code, definitions.GetProperty(entry.Definition))).ToList();
        var conditions = ConditionDefinitions(definitions).Select(entry => ConditionOf(entry.Code, definitions.GetProperty(entry.Definition))).ToList();
        var blockFields = BlockDefinitions.SelectMany(name => FieldsOf(definitions.GetProperty(name))).ToDictionary(field => field.Name);

        return new RuleVocabulary(indicators, conditions, blockFields, reports);
    }

    private static DeclaredDraftReport DeclaredReportOf(StrategyReportDto report)
    {
        var schedule = new ReportTiming(report.Schedule.Frequency, report.Schedule.Day, report.Schedule.Weekday, report.Schedule.Time, report.Schedule.Minute);

        return new DeclaredDraftReport(report.Code, report.Name, report.Description, schedule, report.SignalKinds ?? []);
    }

    private static List<(string Code, string Definition)> IndicatorDefinitions(JsonElement root)
    {
        var patterns = root.GetProperty("properties").GetProperty("indicators").GetProperty("patternProperties");
        var union = patterns.EnumerateObject().First().Value;

        return MappingOf(union);
    }

    private static List<(string Code, string Definition)> ConditionDefinitions(JsonElement definitions)
    {
        var condition = definitions.GetProperty("Gate").GetProperty("properties").GetProperty("condition");

        return MappingOf(condition);
    }

    private static List<(string Code, string Definition)> MappingOf(JsonElement union)
    {
        var mapping = union.GetProperty("discriminator").GetProperty("mapping");

        return [.. mapping.EnumerateObject().Select(entry => (entry.Name, DefinitionName(entry.Value.GetString()!)))];
    }

    private static string DefinitionName(string reference) => reference[(reference.LastIndexOf('/') + 1)..];

    private static IndicatorKind IndicatorOf(string code, JsonElement definition) =>
        new(
            Code: code,
            Label: Text(definition, "x-label"),
            Hint: Text(definition, "x-hint"),
            Phrase: Text(definition, "x-phrase"),
            ColumnFormat: Text(definition, "x-column-format"),
            Fields: FieldsOf(definition));

    private static ConditionKind ConditionOf(string code, JsonElement definition) =>
        new(
            Code: code,
            Label: Text(definition, "x-label"),
            Hint: Text(definition, "x-hint"),
            Phrase: Text(definition, "x-phrase"),
            Fields: FieldsOf(definition));

    private static List<VocabularyField> FieldsOf(JsonElement definition)
    {
        var required = definition.TryGetProperty("required", out var names) ? names.EnumerateArray().Select(name => name.GetString()!).ToHashSet() : [];
        var properties = definition.GetProperty("properties").EnumerateObject().Where(property => !LinkedFieldNames.Contains(property.Name));

        return [.. properties.Select(property => FieldOf(property.Name, property.Value, required.Contains(property.Name)))];
    }

    private static VocabularyField FieldOf(string name, JsonElement property, bool required)
    {
        var numeric = NumericBranch(property);

        return new VocabularyField(
            Name: name,
            Kind: KindOf(name, property, numeric),
            Label: property.TryGetProperty("description", out var description) ? description.GetString()! : "",
            Minimum: Bound(numeric, "minimum", "exclusiveMinimum"),
            Maximum: Bound(numeric, "maximum", "exclusiveMaximum"),
            Required: required,
            Default: property.TryGetProperty("default", out var given) ? given.Clone() : null,
            ChoiceLabels: ChoiceLabelsOf(property));
    }

    private static FieldKind KindOf(string name, JsonElement property, JsonElement numeric)
    {
        if (property.TryGetProperty("$ref", out _))
            return FieldKind.Series;
        if (IndicatorFieldNames.Contains(name))
            return FieldKind.Indicator;
        if (name == "signalId")
            return FieldKind.Signal;
        if (property.TryGetProperty("x-choice-labels", out _))
            return FieldKind.Choice;
        if (property.TryGetProperty("format", out var format) && format.GetString() == "fraction")
            return FieldKind.Fraction;

        return TypeOf(numeric) == "integer" ? FieldKind.Integer : FieldKind.Number;
    }

    private static JsonElement NumericBranch(JsonElement property)
    {
        if (!property.TryGetProperty("anyOf", out var branches))
            return property;

        return branches.EnumerateArray().FirstOrDefault(branch => TypeOf(branch) is "integer" or "number", property);
    }

    private static string? TypeOf(JsonElement schema) =>
        schema.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String ? type.GetString() : null;

    private static double? Bound(JsonElement schema, string inclusive, string exclusive)
    {
        if (schema.TryGetProperty(inclusive, out var bound))
            return bound.GetDouble();

        return schema.TryGetProperty(exclusive, out var open) ? open.GetDouble() : null;
    }

    private static Dictionary<string, string> ChoiceLabelsOf(JsonElement property) =>
        property.TryGetProperty("x-choice-labels", out var labels)
            ? labels.EnumerateObject().ToDictionary(label => label.Name, label => label.Value.GetString()!)
            : [];

    private static string Text(JsonElement definition, string name) =>
        definition.TryGetProperty(name, out var value) ? value.GetString()! : "";
}
