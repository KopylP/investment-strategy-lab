using System.Text.Json;

namespace InvestmentLab.Strategies.Catalog.Domain;

/// <summary>A report a strategy type makes, as its engine declares it: the service lists, schedules and generates it from this.</summary>
/// <param name="Code">Unique within the type, e.g. "rebalance".</param>
/// <param name="Mode">"rebalance" (contribution, buys and sells) or "riskReview" (sells only on an exit signal).</param>
/// <param name="Frequency">"off", "weekly", "monthly" or "quarterly": the type's default.</param>
/// <param name="Time">Local time (HH:mm) on the report's Sunday.</param>
/// <param name="FrequencyParameter">Dotted camelCase path of the parameter that sets the frequency for one strategy; null — always the default.</param>
public sealed record ReportDefinition(string Code, string Name, string Description, string Mode, string Frequency, string Time, string? FrequencyParameter)
{
    private static readonly HashSet<string> Frequencies = ["off", "weekly", "monthly", "quarterly"];

    /// <summary>How often for a strategy with these parameters (JSON): its value of the frequency parameter, else the default.</summary>
    public string FrequencyFor(string parameters)
    {
        if (FrequencyParameter is not { } path)
            return Frequency;
        try
        {
            using var document = JsonDocument.Parse(parameters);
            var value = document.RootElement;
            foreach (var name in path.Split('.'))
            {
                if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(name, out value))
                    return Frequency;
            }
            return value.ValueKind == JsonValueKind.String && value.GetString() is { } frequency && Frequencies.Contains(frequency) ? frequency : Frequency;
        }
        catch (JsonException)
        {
            return Frequency;
        }
    }
}

/// <summary>An algorithm an engine can run: its parameters (JSON Schema + defaults) and the series it needs.</summary>
public sealed class StrategyTypeDescriptor
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Engine { get; private set; }
    public string EngineVersion { get; private set; }
    public bool SupportsState { get; private set; }
    public List<string> RequiredSeriesRoles { get; private set; }

    /// <summary>JSON Schema of the parameters, as published by the engine.</summary>
    public string ParametersSchema { get; private set; }

    /// <summary>Full parameter set with every default, as JSON.</summary>
    public string DefaultParameters { get; private set; }

    /// <summary>The reports the type makes; empty for an engine that declares none.</summary>
    public List<ReportDefinition> Reports { get; private set; }

    public DateTimeOffset SyncedAt { get; private set; }

    private StrategyTypeDescriptor(
        string code, string name, string description, string engine, string engineVersion, bool supportsState,
        List<string> requiredSeriesRoles, string parametersSchema, string defaultParameters, List<ReportDefinition> reports, DateTimeOffset syncedAt)
    {
        Code = code;
        Name = name;
        Description = description;
        Engine = engine;
        EngineVersion = engineVersion;
        SupportsState = supportsState;
        RequiredSeriesRoles = requiredSeriesRoles;
        ParametersSchema = parametersSchema;
        DefaultParameters = defaultParameters;
        Reports = reports;
        SyncedAt = syncedAt;
    }

    public static StrategyTypeDescriptor Of(
        string code, string name, string description, string engine, string engineVersion, bool supportsState,
        IEnumerable<string> requiredSeriesRoles, string parametersSchema, string defaultParameters, IEnumerable<ReportDefinition> reports, DateTimeOffset syncedAt) =>
        new(code, name, description, engine, engineVersion, supportsState, [.. requiredSeriesRoles], parametersSchema, defaultParameters, [.. reports], syncedAt);

    /// <summary>Takes over what the engine publishes now; the code stays.</summary>
    public void RefreshFrom(StrategyTypeDescriptor published)
    {
        Name = published.Name;
        Description = published.Description;
        Engine = published.Engine;
        EngineVersion = published.EngineVersion;
        SupportsState = published.SupportsState;
        RequiredSeriesRoles = [.. published.RequiredSeriesRoles];
        ParametersSchema = published.ParametersSchema;
        DefaultParameters = published.DefaultParameters;
        Reports = [.. published.Reports];
        SyncedAt = published.SyncedAt;
    }
}
