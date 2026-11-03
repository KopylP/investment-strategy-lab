using System.Text.Json;
using System.Text.Json.Nodes;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;
using Json.Schema;

namespace InvestmentLab.Strategies.Definitions.Application.Shared;

public sealed class StrategySetupFactory(IStrategyTypes types, IInstrumentCatalog instruments)
{
    public async Task<Result<StrategySetup>> CreateAsync(string typeCode, StrategySetupDraft draft, CancellationToken cancellationToken)
    {
        if (await types.FindAsync(typeCode, cancellationToken) is not { } type)
            return StrategyErrors.UnknownType(typeCode);

        var known = (await instruments.GetInstruments(cancellationToken)).Select(instrument => instrument.Id).ToHashSet();
        Guid?[
] referenced = [.. (draft.Universe ?? []).Select(id => (Guid?)id),
            draft.BenchmarkInstrumentId,
            draft.VolatilityIndexInstrumentId,
            draft.CashInstrumentId];
        var unknown = referenced.OfType<Guid>().Where(id => !known.Contains(id)).Distinct().ToList();
        if (unknown.Count > 0)
            return StrategyErrors.UnknownInstruments(unknown.Select(id => id.ToString()));

        var parameters = Parameters(type, draft.Parameters);

        return parameters.IsFailure
            ? parameters.Error
            : new StrategySetup(parameters.Value, draft.Universe ?? [], draft.CashInstrumentId,
                new MarketSeriesRefs(draft.BenchmarkInstrumentId, draft.VolatilityIndexInstrumentId));
    }

    public static Result<string> Parameters(StrategyTypeDescriptor type, JsonElement? given)
    {
        var parameters = type.DefaultParameters is null ? AsGiven(given) : MergedIntoDefaults(type.DefaultParameters, given);

        return parameters.IsFailure ? parameters.Error : Validated(type, parameters.Value);
    }

    private static Result<string> AsGiven(JsonElement? given) =>
        given is { ValueKind: JsonValueKind.Object } parameters
            ? parameters.GetRawText()
            : StrategyErrors.InvalidParameters("expected a JSON object.");

    private static Result<string> MergedIntoDefaults(string defaults, JsonElement? given)
    {
        var merged = JsonNode.Parse(defaults)!.AsObject();

        if (given is { ValueKind: JsonValueKind.Object } overrides)
            Merge(merged, JsonNode.Parse(overrides.GetRawText())!.AsObject());
        else if (given is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
            return StrategyErrors.InvalidParameters("expected a JSON object.");

        return merged.ToJsonString();
    }

    private static Result<string> Validated(StrategyTypeDescriptor type, string json)
    {
        var evaluation = JsonSchema.FromText(type.ParametersSchema, new BuildOptions { Dialect = Dialect.Draft202012 })
            .Evaluate(JsonDocument.Parse(json).RootElement, new EvaluationOptions { OutputFormat = OutputFormat.List });
        if (evaluation.IsValid)
            return json;

        var problems = (evaluation.Details ?? [])
            .Where(detail => detail.Errors is { Count: > 0 })
            .Select(detail => $"{detail.InstanceLocation}: {string.Join("; ", detail.Errors!.Values)}")
            .Distinct();
        return StrategyErrors.InvalidParameters(string.Join(" | ", problems));
    }

    private static void Merge(JsonObject target, JsonObject overrides)
    {
        foreach (var (name, value) in overrides)
        {
            if (value is JsonObject nested && target[name] is JsonObject existing)
                Merge(existing, nested);
            else
                target[name] = value?.DeepClone();
        }
    }
}
