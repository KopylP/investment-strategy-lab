namespace InvestmentLab.Strategies.Definitions.Domain;

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
        Raise(new StrategyReconfigured(Id, OwnerId, version.Number, reconfiguredAt));

        return version;
    }

    public void Rename(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
    }

    public void Archive() => Status = StrategyStatus.Archived;

    public StrategyVersion? Version(int? number) =>
        number is { } pinned ? _versions.FirstOrDefault(version => version.Number == pinned) : Latest;

    private static Error? CheckSetup(StrategySetup setup) =>
        setup.Universe.Count == 0 ? StrategyErrors.UniverseEmpty()
        : setup.Universe.Distinct().Count() != setup.Universe.Count ? StrategyErrors.UniverseRepeated()
        : setup.Universe.Contains(Guid.Empty) || setup.Market.BenchmarkInstrumentId == Guid.Empty ? StrategyErrors.InstrumentRequired()
        : null;
}
