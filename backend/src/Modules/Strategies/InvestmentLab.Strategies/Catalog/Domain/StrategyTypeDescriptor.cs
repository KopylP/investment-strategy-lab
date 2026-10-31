namespace InvestmentLab.Strategies.Catalog.Domain;

public sealed class StrategyTypeDescriptor
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Engine { get; private set; }
    public string EngineVersion { get; private set; }
    public bool SupportsState { get; private set; }
    public List<string> RequiredSeriesRoles { get; private set; }

    public string ParametersSchema { get; private set; }

    public string DefaultParameters { get; private set; }

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
        IEnumerable<string> requiredSeriesRoles,
        string parametersSchema,
        string defaultParameters,
        IEnumerable<ReportDefinition> reports,
        DateTimeOffset syncedAt) =>
        new(
            code,
            name,
            description,
            engine,
            engineVersion,
            supportsState,
            [.. requiredSeriesRoles],
            parametersSchema,
            defaultParameters,
            [.. reports],
            syncedAt);

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
