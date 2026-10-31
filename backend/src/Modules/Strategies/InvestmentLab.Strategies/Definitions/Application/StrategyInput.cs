using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application;

public sealed class StrategySetupFactory(IStrategyTypes types, IInstrumentCatalog instruments)
{
    public async Task<Result<StrategySetup>> CreateAsync(string typeCode, StrategySetupInput input, CancellationToken cancellationToken)
    {
        if (await types.FindAsync(typeCode, cancellationToken) is not { } type)
            return StrategyErrors.UnknownType(typeCode);

        var known = (await instruments.GetInstruments(cancellationToken)).Select(instrument => instrument.Id).ToHashSet();
        Guid?[] referenced = [.. (input.Universe ?? []).Select(id => (Guid?)id), input.BenchmarkInstrumentId, input.VolatilityIndexInstrumentId, input.CashInstrumentId];
        var unknown = referenced.OfType<Guid>().Where(id => !known.Contains(id)).Distinct().ToList();
        if (unknown.Count > 0)
            return StrategyErrors.UnknownInstruments(unknown.Select(id => id.ToString()));

        var parameters = Parameters(type, input.Parameters);

        return parameters.IsFailure
            ? parameters.Error
            : new StrategySetup(parameters.Value, input.Universe ?? [], input.CashInstrumentId,
                new MarketSeriesRefs(input.BenchmarkInstrumentId, input.VolatilityIndexInstrumentId));
    }

    public static Result<string> Parameters(StrategyTypeDescriptor type, JsonElement? given)
    {
        var merged = JsonNode.Parse(type.DefaultParameters)!.AsObject();

        if (given is { ValueKind: JsonValueKind.Object } overrides)
            Merge(merged, JsonNode.Parse(overrides.GetRawText())!.AsObject());
        else if (given is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
            return StrategyErrors.InvalidParameters("expected a JSON object.");

        var json = merged.ToJsonString();

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
