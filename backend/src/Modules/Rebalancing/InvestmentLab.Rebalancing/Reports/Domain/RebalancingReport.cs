using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Domain;

public sealed class RebalancingReport : AggregateRoot<ReportId>
{
    private readonly List<ReportAction> _actions = [];

    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string AccountName { get; private set; }
    public Currency Currency { get; private set; }
    public YearMonth Period { get; private set; }
    public ReportKind Kind { get; private set; }

    public string Code { get; private set; }

    public string Title { get; private set; }

    public ReportTrigger Trigger { get; private set; }
    public ReportStatus Status { get; private set; }
    public DateOnly MarketAsOf { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public ContributionPlan Contribution { get; private set; }
    public List<ReportSection> Sections { get; private set; }
    public List<string> Warnings { get; private set; }
    public PlanReview? Review { get; private set; }
    public TaxEstimate? TaxEstimate { get; private set; }
    public ReportId? SupersededBy { get; private set; }
    public IReadOnlyList<ReportAction> Actions => [.. _actions.OrderBy(action => action.Sequence)];

    private RebalancingReport(ReportId id, ReportDraft draft, DateTimeOffset generatedAt) : base(id)
    {
        AccountId = draft.AccountId;
        OwnerId = draft.OwnerId;
        AccountName = draft.AccountName;
        Currency = draft.Currency;
        Period = draft.Period;
        Kind = draft.Kind;
        Code = draft.Code ?? ReportDefaults.CodeOf(draft.Kind);
        Title = draft.Title ?? ReportDefaults.TitleOf(draft.Kind);
        Trigger = draft.Trigger;
        Status = ReportStatus.New;
        MarketAsOf = draft.MarketAsOf;
        GeneratedAt = generatedAt;
        Contribution = draft.Contribution;
        Sections = [.. draft.Sections];
        Warnings = [.. draft.Warnings];
        Review = draft.Review;
        _actions.AddRange(draft.Actions.Select((action, index) => ReportAction.Create(index + 1, action)));
    }

    private RebalancingReport(ReportId id) : base(id) =>
        (AccountName, Code, Title, Currency, Contribution, Sections, Warnings) = (null!, null!, null!, null!, null!, null!, null!);

    public static RebalancingReport Generate(ReportDraft draft, DateTimeOffset generatedAt)
    {
        var report = new RebalancingReport(ReportId.New(), draft, generatedAt);
        if (report._actions.Count == 0)
        {
            report.Status = ReportStatus.Closed;
            report.ClosedAt = generatedAt;
        }

        return report;
    }

    public IEnumerable<ReportAction> Sales => _actions.Where(action => action.Kind is ActionKind.Sell or ActionKind.Unpark);

    public void Generated(DateTimeOffset requestedAt, DateTimeOffset now) => Raise(new ReportGenerated(this, requestedAt, now));

    public void AttachTaxEstimate(TaxEstimate estimate, IReadOnlyDictionary<ReportActionId, SaleTaxImpact> perSale)
    {
        TaxEstimate = estimate;

        foreach (var action in Sales)
            if (perSale.TryGetValue(action.Id, out var sale))
                action.AttachTaxEstimate(sale);
    }

    public bool IsOpen => Status is ReportStatus.New or ReportStatus.InProgress;

    public ReportAction? FindAction(ReportActionId actionId) => _actions.FirstOrDefault(action => action.Id == actionId);

    public Result<MarkedFill> MarkExecuted(
        ReportActionId actionId,
        decimal quantity,
        decimal price,
        decimal fee,
        DateOnly executedOn,
        string? note,
        DateTimeOffset now) =>
        RecordFill(actionId, quantity, price, fee, executedOn, note, final: true, now);

    public Result<MarkedFill> RecordPartialFill(
        ReportActionId actionId,
        decimal quantity,
        decimal price,
        decimal fee,
        DateOnly executedOn,
        string? note,
        DateTimeOffset now) =>
        RecordFill(actionId, quantity, price, fee, executedOn, note, final: false, now);

    public Result LinkJournalOperations(IReadOnlyList<ActionMatch> matches, DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        foreach (var match in matches)
        {
            if (FindAction(match.ActionId) is not { } action)
                return ReportErrors.ActionNotFound(match.ActionId.Value);

            var linked = action.LinkOperations(match.Operations);
            if (linked.IsFailure)
                return linked;
        }

        if (matches.Count > 0)
            StartWorkIfNew();

        CloseWhenAllActionsResolved(now);

        return Result.Success();
    }

    public Result Skip(ReportActionId actionId, string reason, DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        if (FindAction(actionId) is not { } action)
            return ReportErrors.ActionNotFound(actionId.Value);

        var skipped = action.Skip(reason);
        if (skipped.IsFailure)
            return skipped;

        StartWorkIfNew();
        CloseWhenAllActionsResolved(now);

        return Result.Success();
    }

    public Result<IReadOnlyList<MarkedFill>> UndoMark(ReportActionId actionId)
    {
        if (Status == ReportStatus.Superseded)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        if (FindAction(actionId) is not { } action)
            return ReportErrors.ActionNotFound(actionId.Value);

        var undone = action.Undo();
        if (undone.IsFailure)
            return undone.Error;

        if (Status == ReportStatus.Closed)
        {
            Status = ReportStatus.InProgress;
            ClosedAt = null;
        }

        if (_actions.All(each => each.Status == ActionStatus.Pending))
            Status = ReportStatus.New;

        return Result<IReadOnlyList<MarkedFill>>.Success(
            [.. undone.Value.Select(fill => new MarkedFill(action, fill, new FillReference(Id, action.Id, fill.Number)))]);
    }

    public Result<IReadOnlyList<MarkedFill>> UndoMark(ReportActionId actionId, DateTimeOffset now)
    {
        var undone = UndoMark(actionId);
        if (undone.IsSuccess)
            RaiseUndone(undone.Value, now);

        return undone;
    }

    public void ApplyTransactionRecorded(FillReference reference, Guid transactionId) =>
        FindAction(reference.ActionId)?.ApplyRecorded(reference.FillNumber, transactionId);

    public void ApplyTransactionRejected(FillReference reference, string reason)
    {
        if (FindAction(reference.ActionId) is not { } action || !action.ApplyRejected(reference.FillNumber, reason))
            return;

        if (Status == ReportStatus.Closed)
        {
            Status = ReportStatus.InProgress;
            ClosedAt = null;
        }

        if (Status == ReportStatus.InProgress && _actions.All(each => each.Status == ActionStatus.Pending))
            Status = ReportStatus.New;
    }

    public Result Close(DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        foreach (var action in _actions)
            action.CloseUnmarked();

        CloseAndAnnounce(now);

        return Result.Success();
    }

    public Result SupersedeBy(ReportId newer, DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        Status = ReportStatus.Superseded;
        SupersededBy = newer;
        ClosedAt = now;

        Raise(new ReportSuperseded(Id, AccountId, newer, now));

        return Result.Success();
    }

    public void SupersedeOpen(IEnumerable<RebalancingReport> others, DateTimeOffset now)
    {
        foreach (var other in others.Where(Replaces))
            other.SupersedeBy(Id, now);
    }

    private bool Replaces(RebalancingReport other) =>
        other.IsOpen
            && other.Code == Code
            && (Kind == ReportKind.RiskReview ? other.Kind == ReportKind.RiskReview : other.Kind is ReportKind.Monthly or ReportKind.Recalculation);

    public static decimal ContributedIn(IEnumerable<RebalancingReport> reports, string code) =>
        reports.Where(report => report.Code == code).Sum(report => report.ContributedAmount);

    public decimal ContributedAmount =>
        _actions.Where(action => action.Kind == ActionKind.Contribute).SelectMany(action => action.Fills).Sum(fill => fill.Amount);

    private Result<MarkedFill> RecordFill(
        ReportActionId actionId, decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note, bool final, DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        if (FindAction(actionId) is not { } action)
            return ReportErrors.ActionNotFound(actionId.Value);

        if (action.Kind is ActionKind.Contribute or ActionKind.Transfer && !final)
            return ReportErrors.PartialFillNotSupported(action.Kind);

        var fill = action.RecordFill(quantity, price, fee, executedOn, note, final);
        if (fill.IsFailure)
            return fill.Error;

        var marked = new MarkedFill(action, fill.Value, new FillReference(Id, action.Id, fill.Value.Number));

        StartWorkIfNew();
        RaiseMarked(marked, now);
        CloseWhenAllActionsResolved(now);

        return marked;
    }

    private void StartWorkIfNew()
    {
        if (Status == ReportStatus.New)
            Status = ReportStatus.InProgress;
    }

    private void CloseWhenAllActionsResolved(DateTimeOffset now)
    {
        if (IsOpen && _actions.Count > 0 && _actions.All(action => action.IsResolved) && Status == ReportStatus.InProgress)
            CloseAndAnnounce(now);
    }

    private void CloseAndAnnounce(DateTimeOffset now)
    {
        Status = ReportStatus.Closed;
        ClosedAt = now;

        Raise(new ReportClosed(Id, AccountId, OwnerId, now));
    }

    private void RaiseMarked(MarkedFill marked, DateTimeOffset now)
    {
        var (action, fill, reference) = (marked.Action, marked.Fill, marked.Reference.ToString());

        IDomainEvent domainEvent = action.Kind switch
        {
            ActionKind.Contribute =>
                new ContributionMarked(AccountId, reference, fill.Amount, fill.ExecutedOn, action.AllocateFill(fill.Amount), now),
            ActionKind.Transfer => new TransferMarked(
                AccountId,
                reference,
                action.FromSubPortfolioId!.Value,
                action.ToSubPortfolioId!.Value,
                fill.ExecutedOn,
                action.InstrumentId,
                fill.Quantity,
                now),
            _ => new TradeMarked(
                AccountId, reference, action.InstrumentId!.Value, action.Kind is ActionKind.Sell or ActionKind.Unpark,
                fill.Quantity, fill.Price, fill.Fee, fill.ExecutedOn, action.AllocateFill(fill.Quantity), now),
        };

        Raise(domainEvent);
    }

    private void RaiseUndone(IEnumerable<MarkedFill> undone, DateTimeOffset now)
    {
        var inJournalByThisReport = undone.Where(marked => !marked.Fill.Linked);
        foreach (var marked in inJournalByThisReport)
            Raise(new MarkUndone(AccountId, marked.Reference.ToString(), now));
    }
}
