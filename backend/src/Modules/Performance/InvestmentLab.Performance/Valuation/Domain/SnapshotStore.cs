namespace InvestmentLab.Performance.Valuation.Domain;

public enum SnapshotScenario
{
    Actual,

    /// <summary>The shadow portfolio: every plan executed in full.</summary>
    FullPlan,
}

/// <summary>A stored daily snapshot (read model, D9): derived from the journal and quotes, rebuilt when its input changes.</summary>
public sealed class ValuationSnapshot
{
    public long Id { get; private set; }
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public SnapshotScenario Scenario { get; private set; }
    public Guid? SubPortfolioId { get; private set; }
    public DateOnly Date { get; private set; }
    public decimal Value { get; private set; }
    public decimal Cash { get; private set; }
    public decimal Flow { get; private set; }
    public decimal NetContributions { get; private set; }
    public decimal TwrIndex { get; private set; }
    public decimal CostBasis { get; private set; }

    private ValuationSnapshot() { }

    public static ValuationSnapshot From(Guid accountId, UserId ownerId, SnapshotScenario scenario, Snapshot snapshot) => new()
    {
        AccountId = accountId,
        OwnerId = ownerId,
        Scenario = scenario,
        SubPortfolioId = snapshot.SubPortfolioId,
        Date = snapshot.Date,
        Value = snapshot.Value,
        Cash = snapshot.Cash,
        Flow = snapshot.Flow,
        NetContributions = snapshot.NetContributions,
        TwrIndex = snapshot.TwrIndex,
        CostBasis = snapshot.CostBasis,
    };
}

/// <summary>
/// Whether an account's snapshots are current (§6.8 RecalculationMarker). A change back in time moves
/// <see cref="DirtyFrom"/> only backwards; a rebuild clears it.
/// </summary>
public sealed class RecalculationMarker
{
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public DateOnly? DirtyFrom { get; private set; }
    public DateTimeOffset? BuiltAt { get; private set; }
    public DateOnly? BuiltThrough { get; private set; }

    private RecalculationMarker(Guid accountId, UserId ownerId) => (AccountId, OwnerId, DirtyFrom) = (accountId, ownerId, DateOnly.MinValue);

    // EF Core.
    private RecalculationMarker() { }

    public static RecalculationMarker For(Guid accountId, UserId ownerId) => new(accountId, ownerId);

    public bool IsDirty => DirtyFrom is not null;

    public void MarkDirty(DateOnly from)
    {
        if (DirtyFrom is null || from < DirtyFrom)
            DirtyFrom = from;
    }

    public void Built(DateTimeOffset at, DateOnly through) => (DirtyFrom, BuiltAt, BuiltThrough) = (null, at, through);
}
