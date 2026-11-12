using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Drafts.Domain;

public sealed class StrategyDraft : AggregateRoot<Guid>, IUserOwned
{
    public UserId OwnerId { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public DraftSetup Setup { get; private set; }

    public StrategyGraph Graph { get; private set; }

    public DraftLayout Layout { get; private set; }

    public IReadOnlyList<DraftReport> Reports { get; private set; }

    public PublishedOrigin? Origin { get; private set; }

    public Guid? TrialStrategyId { get; private set; }

    public IReadOnlyList<TrialRun> Trials { get; private set; } = [];

    public IReadOnlyList<Guid> MissingSignalIds { get; private set; } = [];

    public int Revision { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    private StrategyDraft(Guid id, UserId ownerId, string name, DraftSetup setup, StrategyGraph graph, IReadOnlyList<DraftReport> reports, DateTimeOffset now)
        : base(id)
    {
        OwnerId = ownerId;
        Name = name;
        Setup = setup;
        Graph = PortfolioFlow.Upgrade(SetupBindings.WithLegacyBindings(graph));
        Layout = DraftLayout.Empty;
        Reports = reports;
        Revision = 1;
        UpdatedAt = now;
    }

    private StrategyDraft(Guid id) : base(id) =>
        (Name, Setup, Graph, Layout, Reports) = (null!, null!, null!, null!, null!);

    public static Result<StrategyDraft> Start(
        UserId owner, string name, DraftSetup setup, StrategyGraph graph, IReadOnlyList<DraftReport> reports, PublishedOrigin? origin, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return DraftErrors.NameRequired();

        return new StrategyDraft(Guid.CreateVersion7(), owner, name.Trim(), setup, graph, reports, now) { Origin = origin };
    }

    public Result Change(DraftChange change, DateTimeOffset now)
    {
        if (change.Revision != Revision)
            return DraftErrors.Stale();
        if (change.Name is not null && string.IsNullOrWhiteSpace(change.Name))
            return DraftErrors.NameRequired();

        Name = change.Name?.Trim() ?? Name;
        Description = change.Description ?? Description;
        Setup = change.Setup ?? Setup;
        Reports = change.Reports ?? Reports;
        if (change.Graph is { } graph)
            ReplaceGraph(graph);

        Revision++;
        UpdatedAt = now;

        return Result.Success();
    }

    public void NormalizeGraph() => Graph = PortfolioFlow.Upgrade(SetupBindings.WithLegacyBindings(Graph));

    public void MoveNodes(DraftLayout layout, DateTimeOffset now)
    {
        Layout = layout.Keeping(Graph);
        UpdatedAt = now;
    }

    public void MarkSignalMissing(Guid signalId)
    {
        if (!MissingSignalIds.Contains(signalId))
            MissingSignalIds = [.. MissingSignalIds, signalId];
    }

    public TrialRun? TrialWith(string fingerprint) =>
        Trials.LastOrDefault(trial => trial.Fingerprint == fingerprint);

    public TrialRun RecordTrial(Guid trialStrategyId, int version, string fingerprint, string? label, DateTimeOffset now)
    {
        var trialVersion = new TrialVersion(trialStrategyId, version);
        var execution = new TrialExecution(trialVersion);

        return RecordTrial(execution, fingerprint, label, now);
    }

    public TrialRun RecordTrial(TrialExecution execution, string fingerprint, string? label, DateTimeOffset now)
    {
        var trial = new TrialRun(
            Number: Trials.Count + 1,
            StrategyId: execution.Version.StrategyId,
            StrategyVersion: execution.Version.Version,
            Fingerprint: fingerprint,
            Label: label,
            RanAt: now,
            ExperimentId: execution.ExperimentId,
            Experiment: execution.Experiment);

        TrialStrategyId ??= execution.Version.StrategyId;
        Trials = [.. Trials, trial];
        UpdatedAt = now;

        return trial;
    }

    private void ReplaceGraph(StrategyGraph graph)
    {
        Graph = PortfolioFlow.Upgrade(SetupBindings.WithLegacyBindings(graph));
        Layout = Layout.Keeping(Graph);
    }
}
