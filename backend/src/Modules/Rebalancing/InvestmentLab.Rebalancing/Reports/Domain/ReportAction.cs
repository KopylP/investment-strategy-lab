namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed class ReportAction : Entity<ReportActionId>
{
    private const decimal QuantityTolerance = 0.0001m;

    public static readonly string ClosedWithoutMark = Message.Of("report.action.closed_without_mark");

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

    public decimal? EstimatedGain { get; private set; }
    public decimal? EstimatedTaxChange { get; private set; }
    public decimal? EstimatedCarriedLoss { get; private set; }

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

    internal Result LinkOperations(IReadOnlyList<JournalOperation> operations)
    {
        if (IsResolved)
            return ReportErrors.ActionAlreadyResolved(Id.Value, Status);

        foreach (var operation in operations)
        {
            var fill = Fill.TryOf(NextFillNumber, operation.Quantity, operation.Price, operation.Fee, operation.OccurredOn, Message.Of("report.action.from_journal"));
            if (fill.IsFailure)
                return fill.Error;

            Fills.Add(fill.Value with { TransactionId = operation.TransactionId, Linked = true });
            NextFillNumber++;
        }

        Rejection = null;
        Status = Kind == ActionKind.Contribute
            || FilledQuantity >= PlannedQuantity - QuantityTolerance
            ? ActionStatus.Executed
            : ActionStatus.PartiallyExecuted;

        return Result.Success();
    }

    internal Result Skip(string reason)
    {
        if (Status != ActionStatus.Pending)
            return ReportErrors.OnlyPendingCanBeSkipped(Id.Value, Status);

        Status = ActionStatus.Skipped;
        SkipReason = string.IsNullOrWhiteSpace(reason) ? Message.Of("report.action.no_reason") : reason.Trim();

        return Result.Success();
    }

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

    internal void AttachTaxEstimate(SaleTaxImpact impact) =>
        (EstimatedGain, EstimatedTaxChange, EstimatedCarriedLoss) = (impact.GainInTaxCurrency, impact.TaxChange, impact.CarriedLoss);

    internal void CloseUnmarked()
    {
        if (Status != ActionStatus.Pending)
            return;

        Status = ActionStatus.Skipped;
        SkipReason = ClosedWithoutMark;
    }

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
