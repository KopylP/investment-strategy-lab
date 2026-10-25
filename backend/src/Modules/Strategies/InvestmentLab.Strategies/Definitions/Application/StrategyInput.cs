using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Strategies.Catalog.Domain;
using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Definitions.Application;

/// <param name="Parameters">Only what differs from the type's defaults; null keeps all defaults.</param>
/// <param name="RebalanceFrequency">Monthly (default), Weekly or Quarterly.</param>
public sealed record StrategySetupInput(
    IReadOnlyList<Guid> Universe,
    Guid BenchmarkInstrumentId,
    Guid? VolatilityIndexInstrumentId = null,
    Guid? CashInstrumentId = null,
    string? RebalanceFrequency = null,
    JsonElement? Parameters = null);

/// <summary>Turns user input into a valid <see cref="StrategySetup"/>: known instruments, parameters merged over defaults and checked against the type's JSON Schema.</summary>
public sealed class StrategySetupFactory(IStrategyTypes types, IInstrumentCatalog instruments)
{
    public async Task<Result<StrategySetup>> CreateAsync(string typeCode, StrategySetupInput input, CancellationToken cancellationToken)
    {
        if (await types.FindAsync(typeCode, cancellationToken) is not { } type)
            return StrategyErrors.UnknownType(typeCode);

        var frequency = RebalanceFrequency.Monthly;
        if (input.RebalanceFrequency is { } text && (!Enum.TryParse(text, ignoreCase: true, out frequency) || !Enum.IsDefined(frequency)))
            return Error.Validation("strategy.frequency_unknown", $"'{text}' is not Monthly or Weekly.");

        var known = (await instruments.GetInstruments(cancellationToken)).Select(instrument => instrument.Id).ToHashSet();
        Guid?[] referenced = [.. (input.Universe ?? []).Select(id => (Guid?)id), input.BenchmarkInstrumentId, input.VolatilityIndexInstrumentId, input.CashInstrumentId];
        var unknown = referenced.OfType<Guid>().Where(id => !known.Contains(id)).Distinct().ToList();
        if (unknown.Count > 0)
            return StrategyErrors.UnknownInstruments(unknown.Select(id => id.ToString()));

        var parameters = Parameters(type, input.Parameters);
        return parameters.IsFailure
            ? parameters.Error
            : new StrategySetup(parameters.Value, input.Universe ?? [], input.CashInstrumentId,
                new MarketSeriesRefs(input.BenchmarkInstrumentId, input.VolatilityIndexInstrumentId), frequency);
    }

    /// <summary>Defaults of the type with the given values laid over them, valid against the type's schema.</summary>
    public static Result<string> Parameters(StrategyTypeDescriptor type, JsonElement? given)
    {
        var merged = JsonNode.Parse(type.DefaultParameters)!.AsObject();
        if (given is { ValueKind: JsonValueKind.Object } overrides)
            Merge(merged, JsonNode.Parse(overrides.GetRawText())!.AsObject());
        else if (given is { ValueKind: not (JsonValueKind.Null or JsonValueKind.Undefined) })
            return StrategyErrors.InvalidParameters("expected a JSON object.");

        var json = merged.ToJsonString();
        var evaluation = JsonSchema.FromText(type.ParametersSchema)
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

public sealed record StrategyVersionDto(
    int Number,
    JsonElement Parameters,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    Guid BenchmarkInstrumentId,
    Guid? VolatilityIndexInstrumentId,
    string RebalanceFrequency,
    DateTimeOffset CreatedAt);

/// <param name="Version">The requested version (the latest by default).</param>
public sealed record StrategyDto(
    Guid Id,
    string Name,
    string TypeCode,
    string Status,
    DateTimeOffset CreatedAt,
    int LatestVersion,
    StrategyVersionDto Version);

internal static class StrategyMapping
{
    extension(Strategy strategy)
    {
        public StrategyDto ToDto(StrategyVersion? version = null)
        {
            var shown = version ?? strategy.Latest;
            return new StrategyDto(
                strategy.Id.Value,
                strategy.Name,
                strategy.TypeCode,
                strategy.Status.ToString(),
                strategy.CreatedAt,
                strategy.Latest.Number,
                new StrategyVersionDto(
                    shown.Number,
                    JsonDocument.Parse(shown.Parameters).RootElement,
                    shown.Universe,
                    shown.CashInstrumentId,
                    shown.BenchmarkInstrumentId,
                    shown.VolatilityIndexInstrumentId,
                    shown.Frequency.ToString(),
                    shown.CreatedAt));
        }
    }
}
