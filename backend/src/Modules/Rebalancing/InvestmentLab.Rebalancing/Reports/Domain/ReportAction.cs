namespace InvestmentLab.Rebalancing.Reports.Domain;

/// <summary>What is needed to create an action when a report is generated.</summary>
public sealed record ActionDraft(
    ActionKind Kind,
    Guid? InstrumentId,
    decimal PlannedQuantity,
    decimal PlannedPrice,
    IReadOnlyList<ActionAllocation> Allocations,
    string Reason,
    Guid? FromSubPortfolioId = null,
    Guid? ToSubPortfolioId = null);

/// <summary>
/// One step of a report at broker level (after netting): pay in, sell, buy, park, unpark or move between sub-portfolios.
/// It is marked with fills; the journal changes only through those fills (D7).
/// </summary>
public sealed class ReportAction : Entity<ReportActionId>
{
    /// <summary>Fills within this many units of the plan count as the whole plan (fractional-share rounding).</summary>
    private const decimal QuantityTolerance = 0.0001m;

    public const string ClosedWithoutMark = "закрито без відмітки";

    public int Sequence { get; private set; }
    public ActionKind Kind { get; private set; }
    public Guid? InstrumentId { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal PlannedPrice { get; private set; }
    public decimal PlannedAmount { get; private set; }
    public List<ActionAllocation> Allocations { get; private set; }
    public Guid? FromSubPortfolioId { get; private set; }
    public Guid? ToSubPortfolioId { get; private set; }
    public string Reason { get; private set; }
    public ActionStatus Status { get; private set; }
    public List<Fill> Fills { get; private set; }
    public int NextFillNumber { get; private set; }
    public string? SkipReason { get; private set; }

    /// <summary>Gain and tax (PLN) this sale would realise, estimated when the report was generated.</summary>
    public decimal? EstimatedGainPln { get; private set; }
    public decimal? EstimatedTaxPln { get; private set; }

    /// <summary>Why Portfolio refused the last fill; shown to the user until the action is marked again.</summary>
    public string? Rejection { get; private set; }

    private ReportAction(ReportActionId id, int sequence, ActionDraft draft) : base(id)
    {
        Sequence = sequence;
        Kind = draft.Kind;
        InstrumentId = draft.InstrumentId;
        PlannedQuantity = draft.PlannedQuantity;
        PlannedPrice = draft.PlannedPrice;
        PlannedAmount = decimal.Round(draft.PlannedQuantity * draft.PlannedPrice, 2);
        Allocations = [.. draft.Allocations];
        FromSubPortfolioId = draft.FromSubPortfolioId;
        ToSubPortfolioId = draft.ToSubPortfolioId;
        Reason = draft.Reason;
        Status = ActionStatus.Pending;
        Fills = [];
        NextFillNumber = 1;
    }

    // EF Core.
    private ReportAction(ReportActionId id) : base(id) => (Allocations, Reason, Fills) = (null!, null!, null!);

    internal static ReportAction Create(int sequence, ActionDraft draft) => new(ReportActionId.New(), sequence, draft);

    public decimal FilledQuantity => Fills.Sum(fill => fill.Quantity);

    public decimal RemainingQuantity => Math.Max(0m, PlannedQuantity - FilledQuantity);

    public bool IsResolved => Status is ActionStatus.Executed or ActionStatus.Skipped;

    public bool IsMarked => Status != ActionStatus.Pending;

    internal Result<Fill> RecordFill(decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note, bool final)
    {
        if (IsResolved)
            return ReportErrors.ActionAlreadyResolved(Id.Value, Status);

        var fill = Fill.TryOf(NextFillNumber, quantity, price, fee, executedOn, note);
        if (fill.IsFailure)
            return fill.Error;

        Fills.Add(fill.Value);
        NextFillNumber++;
        Rejection = null;
        Status = final || FilledQuantity >= PlannedQuantity - QuantityTolerance ? ActionStatus.Executed : ActionStatus.PartiallyExecuted;
        return fill.Value;
    }

    /// <summary>Marks the action with operations already in the journal; nothing new is recorded there.</summary>
    internal Result LinkOperations(IReadOnlyList<JournalOperation> operations)
    {
        if (IsResolved)
            return ReportErrors.ActionAlreadyResolved(Id.Value, Status);

        foreach (var operation in operations)
        {
            var fill = Fill.TryOf(NextFillNumber, operation.Quantity, operation.Price, operation.Fee, operation.OccurredOn, "з журналу");
            if (fill.IsFailure)
                return fill.Error;
            Fills.Add(fill.Value with { TransactionId = operation.TransactionId, Linked = true });
            NextFillNumber++;
        }

        Rejection = null;
        Status = Kind == ActionKind.Contribute || FilledQuantity >= PlannedQuantity - QuantityTolerance ? ActionStatus.Executed : ActionStatus.PartiallyExecuted;
        return Result.Success();
    }

    internal Result Skip(string reason)
    {
        if (Status != ActionStatus.Pending)
            return ReportErrors.OnlyPendingCanBeSkipped(Id.Value, Status);

        Status = ActionStatus.Skipped;
        SkipReason = string.IsNullOrWhiteSpace(reason) ? "без причини" : reason.Trim();
        return Result.Success();
    }

    /// <summary>Back to pending; returns the fills whose journal entries must be reversed.</summary>
    internal Result<IReadOnlyList<Fill>> Undo()
    {
        if (Status == ActionStatus.Pending)
            return ReportErrors.NothingToUndo(Id.Value);

        IReadOnlyList<Fill> undone = [.. Fills];
        Fills = [];
        Status = ActionStatus.Pending;
        SkipReason = null;
        Rejection = null;
        return Result<IReadOnlyList<Fill>>.Success(undone);
    }

    internal void ApplyRecorded(int fillNumber, Guid transactionId)
    {
        var index = Fills.FindIndex(fill => fill.Number == fillNumber);
        if (index >= 0)
            Fills[index] = Fills[index] with { TransactionId = transactionId };
    }

    /// <summary>Portfolio could not record the fill: it is taken back and the user sees why.</summary>
    internal bool ApplyRejected(int fillNumber, string reason)
    {
        var index = Fills.FindIndex(fill => fill.Number == fillNumber);
        if (index < 0)
            return false;

        Fills.RemoveAt(index);
        Rejection = reason;
        Status = Fills.Count == 0 ? ActionStatus.Pending : ActionStatus.PartiallyExecuted;
        return true;
    }

    internal void AttachTaxEstimate(decimal gainPln, decimal taxPln) => (EstimatedGainPln, EstimatedTaxPln) = (gainPln, taxPln);

    internal void CloseUnmarked()
    {
        if (Status != ActionStatus.Pending)
            return;
        Status = ActionStatus.Skipped;
        SkipReason = ClosedWithoutMark;
    }

    /// <summary>
    /// Splits a filled quantity between the sub-portfolios in proportion to the plan; what rounding leaves goes to the
    /// largest part, so the parts add up to the fill exactly.
    /// </summary>
    public IReadOnlyList<ActionAllocation> AllocateFill(decimal quantity)
    {
        var planned = Allocations.Where(allocation => allocation.Quantity > 0m).ToList();
        if (planned.Count == 0)
            return [];

        var total = planned.Sum(allocation => allocation.Quantity);
        var parts = planned.Select(allocation => allocation with { Quantity = decimal.Round(quantity * allocation.Quantity / total, 8) }).ToList();
        var rest = quantity - parts.Sum(part => part.Quantity);
        if (rest != 0m)
        {
            var largest = parts.Index().MaxBy(entry => entry.Item.Quantity).Index;
            parts[largest] = parts[largest] with { Quantity = parts[largest].Quantity + rest };
        }
        return [.. parts.Where(part => part.Quantity > 0m)];
    }
}
