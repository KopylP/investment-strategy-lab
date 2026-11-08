namespace InvestmentLab.Strategies.Catalog.Domain;

public sealed class StrategyTypeDescriptor
{
    public string Code { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Engine { get; private set; }
    public string EngineVersion { get; private set; }
    public bool SupportsState { get; private set; }
    public bool SupportsBacktest { get; private set; } = true;
    public List<string> BacktestComparisons { get; private set; } = [];
    public List<string> RequiredSeriesRoles { get; private set; }

    public string ParametersSchema { get; private set; }

    public string? DefaultParameters { get; private set; }

    public List<ReportDefinition> Reports { get; private set; }

    public Dictionary<string, Dictionary<string, string>> Messages { get; private set; } = [];

    public DateTimeOffset SyncedAt { get; private set; }

    private StrategyTypeDescriptor(
        string code, string name, string description, string engine, string engineVersion, bool supportsState,
        List<string> requiredSeriesRoles, string parametersSchema, string? defaultParameters, List<ReportDefinition> reports, DateTimeOffset syncedAt)
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
        string? defaultParameters,
        IEnumerable<ReportDefinition> reports,
        DateTimeOffset syncedAt,
        IReadOnlyDictionary<string, Dictionary<string, string>>? messages = null,
        bool supportsBacktest = true,
        IEnumerable<string>? backtestComparisons = null) =>
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
            syncedAt)
        {
            Messages = messages is null ? [] : new Dictionary<string, Dictionary<string, string>>(messages),
            SupportsBacktest = supportsBacktest,
            BacktestComparisons = [.. backtestComparisons ?? []],
        };

    public void RefreshFrom(StrategyTypeDescriptor published)
    {
        Name = published.Name;
        Description = published.Description;
        Engine = published.Engine;
        EngineVersion = published.EngineVersion;
        SupportsState = published.SupportsState;
        SupportsBacktest = published.SupportsBacktest;
        BacktestComparisons = [.. published.BacktestComparisons];
        RequiredSeriesRoles = [.. published.RequiredSeriesRoles];
        ParametersSchema = published.ParametersSchema;
        DefaultParameters = published.DefaultParameters;
        Reports = [.. published.Reports];
        Messages = new Dictionary<string, Dictionary<string, string>>(published.Messages);
        SyncedAt = published.SyncedAt;
    }

    public IReadOnlyDictionary<string, string> MessagesIn(string languageCode) => Messages.GetValueOrDefault(languageCode) ?? [];
}
