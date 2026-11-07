namespace InvestmentLab.Strategies.Definitions.Domain;

public sealed class Strategy : AggregateRoot<StrategyId>
{
    private readonly List<StrategyVersion> _versions = [];

    public List<Guid> DeletedSignalIds { get; private set; } = [];

    public UserId OwnerId { get; private set; }
    public string Name { get; private set; }
    public string TypeCode { get; private set; }
    public StrategyStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<StrategyVersion> Versions => [.. _versions.OrderBy(version => version.Number)];

    public StrategyVersion Latest => _versions.MaxBy(version => version.Number)!;

    public bool IsTrial => Status == StrategyStatus.Trial;

    public bool IsListed => !IsTrial;

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

    public static Result<Strategy> CreateTrial(UserId ownerId, string typeCode, StrategySetup setup, DateTimeOffset createdAt)
    {
        if (CheckSetup(setup) is { } invalid)
            return invalid;

        var id = StrategyId.New();
        var strategy = new Strategy(id, ownerId, TrialName(id), typeCode, createdAt) { Status = StrategyStatus.Trial };
        strategy._versions.Add(StrategyVersion.Create(1, setup, createdAt));
        strategy.Raise(new StrategyReconfigured(id, ownerId, 1, createdAt, IsTrial: true));

        return strategy;
    }

    public Result<StrategyVersion> ReconfigureTrial(StrategySetup setup, DateTimeOffset reconfiguredAt) =>
        IsTrial ? Reconfigure(setup, reconfiguredAt) : StrategyErrors.NotTrial(Id.Value);

    public Result Delete(DateTimeOffset deletedAt)
    {
        if (!IsTrial)
            return StrategyErrors.NotTrial(Id.Value);

        Raise(new StrategyDeleted(Id, OwnerId, deletedAt));

        return Result.Success();
    }

    public Result<StrategyVersion> Reconfigure(StrategySetup setup, DateTimeOffset reconfiguredAt)
    {
        if (Status == StrategyStatus.Archived)
            return StrategyErrors.Archived(Id.Value);

        if (CheckSetup(setup) is { } invalid)
            return invalid;

        var version = StrategyVersion.Create(Latest.Number + 1, setup, reconfiguredAt);
        _versions.Add(version);
        DeletedSignalIds.RemoveAll(id => !ReferencesSignal(id));
        Raise(new StrategyReconfigured(Id, OwnerId, version.Number, reconfiguredAt, IsTrial));

        return version;
    }

    public bool MarkSignalDeleted(Guid signalId)
    {
        if (!ReferencesSignal(signalId) || DeletedSignalIds.Contains(signalId))
            return false;

        DeletedSignalIds.Add(signalId);

        return true;
    }

    private bool ReferencesSignal(Guid signalId) =>
        Latest.SignalIds.Contains(signalId) || Latest.Reports.Any(report => report.SignalIds.Contains(signalId));

    public void Rename(string name)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name.Trim();
    }

    public void Archive() => Status = StrategyStatus.Archived;

    public StrategyVersion? Version(int? number) =>
        number is { } pinned ? _versions.FirstOrDefault(version => version.Number == pinned) : Latest;

    private static string TrialName(StrategyId id) => $"trial-{id.Value:N}";

    private static Error? CheckSetup(StrategySetup setup) =>
        setup.Universe.Count == 0 ? StrategyErrors.UniverseEmpty()
        : setup.Universe.Distinct().Count() != setup.Universe.Count ? StrategyErrors.UniverseRepeated()
        : setup.Universe.Contains(Guid.Empty) || setup.Market.BenchmarkInstrumentId == Guid.Empty ? StrategyErrors.InstrumentRequired()
        : HasRepeatedExtraSeries(setup) ? StrategyErrors.ExtraSeriesRepeated()
        : null;

    private static bool HasRepeatedExtraSeries(StrategySetup setup)
    {
        var extra = setup.ExtraSeries ?? [];

        return extra.Distinct().Count() != extra.Count;
    }
}
