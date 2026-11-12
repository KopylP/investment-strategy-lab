namespace InvestmentLab.Performance.Valuation.Domain;

public sealed class RecalculationMarker : IUserOwned
{
    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public DateOnly? DirtyFrom { get; private set; }
    public DateTimeOffset? BuiltAt { get; private set; }
    public DateOnly? BuiltThrough { get; private set; }

    private RecalculationMarker(Guid accountId, UserId ownerId) => (AccountId, OwnerId, DirtyFrom) = (accountId, ownerId, DateOnly.MinValue);

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
