namespace InvestmentLab.Strategies.Catalog.Domain;

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

    public DateTimeOffset SyncedAt { get; private set; }

    private StrategyTypeDescriptor(
        string code, string name, string description, string engine, string engineVersion, bool supportsState,
        List<string> requiredSeriesRoles, string parametersSchema, string defaultParameters, DateTimeOffset syncedAt)
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
        SyncedAt = syncedAt;
    }

    public static StrategyTypeDescriptor Of(
        string code, string name, string description, string engine, string engineVersion, bool supportsState,
        IEnumerable<string> requiredSeriesRoles, string parametersSchema, string defaultParameters, DateTimeOffset syncedAt) =>
        new(code, name, description, engine, engineVersion, supportsState, [.. requiredSeriesRoles], parametersSchema, defaultParameters, syncedAt);

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
        SyncedAt = published.SyncedAt;
    }
}
