namespace InvestmentLab.Strategies.Definitions.Domain;

public readonly record struct StrategyId
{
    public Guid Value { get; }

    private StrategyId(Guid value) => Value = value;

    public static StrategyId New() => new(Guid.CreateVersion7());

    public static StrategyId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty strategy id.", nameof(value));

    public override string ToString() => Value.ToString();
}

public enum StrategyStatus
{
    Active,
    Archived,
}

/// <summary>Market series a strategy reads besides its universe: the benchmark (trend filter) and a volatility index.</summary>
public sealed record MarketSeriesRefs(Guid BenchmarkInstrumentId, Guid? VolatilityIndexInstrumentId);

/// <param name="Parameters">Full parameter set (defaults filled in), JSON, already valid for the strategy type.</param>
/// <param name="Universe">Instruments the strategy may pick from.</param>
/// <param name="CashInstrumentId">ETF where money not invested is parked.</param>
public sealed record StrategySetup(
    string Parameters,
    IReadOnlyList<Guid> Universe,
    Guid? CashInstrumentId,
    MarketSeriesRefs Market);

/// <summary>An immutable snapshot of how the strategy was configured; reports point to one.</summary>
public sealed class StrategyVersion
{
    public Guid Id { get; private set; }
    public int Number { get; private set; }
    public string Parameters { get; private set; }
    public List<Guid> Universe { get; private set; }
    public Guid? CashInstrumentId { get; private set; }
    public Guid BenchmarkInstrumentId { get; private set; }
    public Guid? VolatilityIndexInstrumentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private StrategyVersion(int number, StrategySetup setup, DateTimeOffset createdAt)
    {
        Id = Guid.CreateVersion7();
        Number = number;
        Parameters = setup.Parameters;
        Universe = [.. setup.Universe];
        CashInstrumentId = setup.CashInstrumentId;
        BenchmarkInstrumentId = setup.Market.BenchmarkInstrumentId;
        VolatilityIndexInstrumentId = setup.Market.VolatilityIndexInstrumentId;
        CreatedAt = createdAt;
    }

    // EF Core.
    private StrategyVersion() => (Parameters, Universe) = (null!, null!);

    internal static StrategyVersion Create(int number, StrategySetup setup, DateTimeOffset createdAt) => new(number, setup, createdAt);
}

/// <summary>
/// A configured strategy of a user. Changing parameters or universe creates a new version;
/// earlier versions never change, so past reports stay explainable.
/// </summary>
public sealed class Strategy : AggregateRoot<StrategyId>
{
    private readonly List<StrategyVersion> _versions = [];

    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public string TypeCode { get; private set; }
    public StrategyStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<StrategyVersion> Versions => [.. _versions.OrderBy(version => version.Number)];

    public StrategyVersion Latest => _versions.MaxBy(version => version.Number)!;

    private Strategy(StrategyId id, UserId ownerId, string name, string typeCode, DateTimeOffset createdAt) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        TypeCode = typeCode;
        Status = StrategyStatus.Active;
        CreatedAt = createdAt;
    }

    // EF Core.
    private Strategy(StrategyId id) : base(id) => (Name, TypeCode) = (null!, null!);

    public static Result<Strategy> Create(UserId ownerId, string name, string typeCode, StrategySetup setup, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(name))
            return StrategyErrors.NameRequired();
        if (CheckSetup(setup) is { } invalid)
            return invalid;

        var strategy = new Strategy(StrategyId.New(), ownerId, name.Trim(), typeCode, createdAt);
        strategy._versions.Add(StrategyVersion.Create(1, setup, createdAt));
        return strategy;
    }

    public Result<StrategyVersion> Reconfigure(StrategySetup setup, DateTimeOffset reconfiguredAt)
    {
        if (Status == StrategyStatus.Archived)
            return StrategyErrors.Archived(Id.Value);
        if (CheckSetup(setup) is { } invalid)
            return invalid;

        var version = StrategyVersion.Create(Latest.Number + 1, setup, reconfiguredAt);
        _versions.Add(version);
        return version;
    }

    public void Rename(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
    }

    public void Archive() => Status = StrategyStatus.Archived;

    /// <summary>A pinned version, or the latest one when <paramref name="number"/> is null.</summary>
    public StrategyVersion? Version(int? number) =>
        number is { } pinned ? _versions.FirstOrDefault(version => version.Number == pinned) : Latest;

    private static Error? CheckSetup(StrategySetup setup) =>
        setup.Universe.Count == 0 ? StrategyErrors.UniverseEmpty()
        : setup.Universe.Distinct().Count() != setup.Universe.Count ? StrategyErrors.UniverseRepeated()
        : setup.Universe.Contains(Guid.Empty) || setup.Market.BenchmarkInstrumentId == Guid.Empty ? StrategyErrors.InstrumentRequired()
        : null;
}

public static class StrategyErrors
{
    public static Error NotFound(Guid strategyId) => Error.NotFound("strategy.not_found", $"Strategy {strategyId} does not exist.");

    public static Error VersionNotFound(Guid strategyId, int version) =>
        Error.NotFound("strategy.version_not_found", $"Strategy {strategyId} has no version {version}.");

    public static Error NameRequired() => Error.Validation("strategy.name_required", "Name must not be blank.");

    public static Error NameTaken(string name) => Error.Conflict("strategy.name_taken", $"You already have a strategy called '{name}'.");

    public static Error UniverseEmpty() => Error.Validation("strategy.universe_empty", "The universe needs at least one instrument.");

    public static Error UniverseRepeated() => Error.Validation("strategy.universe_repeated", "Each instrument may appear in the universe once.");

    public static Error InstrumentRequired() => Error.Validation("strategy.instrument_required", "Instrument ids must not be empty.");

    public static Error UnknownInstruments(IEnumerable<string> keys) =>
        Error.Validation("strategy.instrument_unknown", $"Unknown instruments: {string.Join(", ", keys)}.");

    public static Error UnknownType(string code) => Error.Validation("strategy.type_unknown", $"No engine runs the strategy type '{code}'.");

    public static Error InvalidParameters(string details) => Error.Validation("strategy.parameters_invalid", $"Parameters do not fit the strategy type: {details}");

    public static Error Archived(Guid strategyId) => Error.Conflict("strategy.archived", $"Strategy {strategyId} is archived.");
}
