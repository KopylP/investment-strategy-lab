namespace InvestmentLab.Strategies.Catalog.Domain;

/// <summary>A report a strategy type makes, as its engine declares it: the service lists, schedules and generates it from this.</summary>
/// <param name="Code">Unique within the type, e.g. "rebalance".</param>
/// <param name="Mode">"rebalance" (contribution, buys and sells) or "riskReview" (sells only on an exit signal).</param>
/// <param name="Schedule">When it comes unless the user sets their own time for the account; null in a catalog synced before schedules existed — off until the next sync.</param>
public sealed record ReportDefinition(string Code, string Name, string Description, string Mode, ReportSchedule? Schedule);

/// <summary>When a report comes, in the service's time zone; each frequency reads only its own fields.</summary>
/// <param name="Frequency">"off", "hourly", "daily", "weekly", "monthly" or "quarterly".</param>
/// <param name="Day">Monthly: day of the month (1–31); quarterly: of the quarter's first month. A shorter month: its last day.</param>
/// <param name="Weekday">Weekly: "monday" … "sunday".</param>
/// <param name="Time">Daily, weekly, monthly, quarterly: local time, HH:mm.</param>
/// <param name="Minute">Hourly: the minute of every hour (0–59).</param>
public sealed record ReportSchedule(string Frequency, int Day, string Weekday, string Time, int Minute)
{
    public static readonly ReportSchedule Off = new("off", 1, "sunday", "20:00", 0);
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
