namespace InvestmentLab.Rebalancing.Reports.Domain;

public readonly record struct ReportId
{
    public Guid Value { get; }

    private ReportId(Guid value) => Value = value;

    public static ReportId New() => new(Guid.CreateVersion7());

    public static ReportId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report id.", nameof(value));

    public override string ToString() => Value.ToString();
}

public readonly record struct ReportActionId
{
    public Guid Value { get; }

    private ReportActionId(Guid value) => Value = value;

    public static ReportActionId New() => new(Guid.CreateVersion7());

    public static ReportActionId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report action id.", nameof(value));

    public override string ToString() => Value.ToString();
}

public enum ReportKind
{
    Monthly,
    Recalculation,

    /// <summary>Weekly: sells only for exit signals, warnings for the rest.</summary>
    RiskReview,
}

public enum ReportTrigger
{
    Scheduled,
    OnDemand,
}

public enum ReportStatus
{
    New,
    InProgress,
    Closed,
    Superseded,
}

public enum ActionKind
{
    /// <summary>Pay the contribution into the account.</summary>
    Contribute,
    Sell,
    Unpark,

    /// <summary>Move units between sub-portfolios instead of selling in one and buying in another.</summary>
    Transfer,
    Buy,
    Park,
}

public enum ActionStatus
{
    Pending,
    PartiallyExecuted,
    Executed,
    Skipped,
}

/// <summary>
/// What was really done for an action. For <see cref="ActionKind.Contribute"/> the quantity is the amount and the
/// price is 1. <see cref="TransactionId"/> is set once Portfolio has recorded the fill in the journal. A
/// <see cref="Linked"/> fill was not sent to the journal: it points at an entry that was already there (an imported
/// statement row), so undoing it only unlinks.
/// </summary>
public sealed record Fill(int Number, decimal Quantity, decimal Price, decimal Fee, DateOnly ExecutedOn, string? Note, Guid? TransactionId, bool Linked = false)
{
    public decimal Amount => decimal.Round(Quantity * Price, 2);

    public static Result<Fill> TryOf(int number, decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note)
    {
        if (quantity <= 0m)
            return ReportErrors.FillQuantityNotPositive();
        if (price <= 0m)
            return ReportErrors.FillPriceNotPositive();
        if (fee < 0m)
            return ReportErrors.FillFeeNegative();
        return new Fill(number, quantity, price, fee, executedOn, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), null);
    }
}

/// <summary>Planned share of a (netted) action that belongs to one sub-portfolio.</summary>
public sealed record ActionAllocation(Guid SubPortfolioId, decimal Quantity, string? Reason);

/// <summary>Contribution of the month and how it is split between sub-portfolios.</summary>
public sealed record ContributionPlan(decimal Total, string Split, IReadOnlyList<ContributionPart> Parts, TransferHint? Transfer)
{
    public static ContributionPlan None => new(0m, "None", [], null);
}

public sealed record ContributionPart(Guid SubPortfolioId, decimal Amount);

/// <summary>Sub-portfolios drifted beyond the band: move value from one to the other (a suggestion, not an action).</summary>
public sealed record TransferHint(Guid FromSubPortfolioId, Guid ToSubPortfolioId, decimal Amount);

/// <summary>Reference that ties a journal entry in Portfolio to a fill of a report action.</summary>
public sealed record FillReference(ReportId ReportId, ReportActionId ActionId, int FillNumber)
{
    private const string Prefix = "rebalancing";

    public override string ToString() => $"{Prefix}:{ReportId}:{ActionId}:{FillNumber}";

    public static FillReference? TryParse(string? text) =>
        text?.Split(':') is [Prefix, var report, var action, var number]
        && Guid.TryParse(report, out var reportId) && reportId != Guid.Empty
        && Guid.TryParse(action, out var actionId) && actionId != Guid.Empty
        && int.TryParse(number, out var fillNumber)
            ? new FillReference(ReportId.Of(reportId), ReportActionId.Of(actionId), fillNumber)
            : null;
}

/// <summary>Estimated tax of the sells in the plan (PLN), from Taxation. An estimate: the broker's PIT-8C decides.</summary>
public sealed record TaxEstimate(string PolicyCode, bool IsTaxed, decimal GainPln, decimal TaxPln, decimal? FxRate, IReadOnlyList<string> Notes);
