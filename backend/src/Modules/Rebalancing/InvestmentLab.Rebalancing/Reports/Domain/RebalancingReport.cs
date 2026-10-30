using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Domain;

/// <summary>Everything a freshly generated report consists of.</summary>
public sealed record ReportDraft(
    Guid AccountId,
    UserId OwnerId,
    string AccountName,
    Currency Currency,
    YearMonth Period,
    ReportKind Kind,
    ReportTrigger Trigger,
    DateOnly MarketAsOf,
    ContributionPlan Contribution,
    IReadOnlyList<ReportSection> Sections,
    IReadOnlyList<ActionDraft> Actions,
    IReadOnlyList<string> Warnings,
    PlanReview? Review,
    string? Code = null,
    string? Title = null);

/// <summary>The reports there were before strategies declared their own: what reports made then are called.</summary>
public static class ReportDefaults
{
    public static string CodeOf(ReportKind kind) => kind == ReportKind.RiskReview ? "risk-review" : "rebalance";

    public static string TitleOf(ReportKind kind) => kind == ReportKind.RiskReview ? "Перевірка ризику" : "Ребаланс";
}


/// <summary>A fill that Portfolio has to record, or (when undone) reverse.</summary>
public sealed record MarkedFill(ReportAction Action, Fill Fill, FillReference Reference);

/// <summary>
/// The working document of a period: actions to take at the broker, marked one by one with what was really done.
/// New → InProgress at the first mark → Closed when every action is executed or skipped (or by hand). A newer report
/// for the same period supersedes an open one.
/// </summary>
public sealed class RebalancingReport : AggregateRoot<ReportId>
{
    private readonly List<ReportAction> _actions = [];

    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string AccountName { get; private set; }
    public Currency Currency { get; private set; }
    public YearMonth Period { get; private set; }
    public ReportKind Kind { get; private set; }

    /// <summary>Which of the strategies' reports this is (their declaration's code), e.g. "rebalance".</summary>
    public string Code { get; private set; }

    /// <summary>The report's name as the strategy declared it when it was made.</summary>
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

    // EF Core.
    private RebalancingReport(ReportId id) : base(id) =>
        (AccountName, Code, Title, Currency, Contribution, Sections, Warnings) = (null!, null!, null!, null!, null!, null!, null!);

    public static RebalancingReport Generate(ReportDraft draft, DateTimeOffset generatedAt)
    {
        var report = new RebalancingReport(ReportId.New(), draft, generatedAt);
        if (report._actions.Count == 0)
        {
            // Nothing to do this time: the report is kept as a record and closed at once.
            report.Status = ReportStatus.Closed;
            report.ClosedAt = generatedAt;
        }
        return report;
    }

    /// <summary>Sells (and unparking) whose tax should be estimated, at their planned numbers.</summary>
    public IEnumerable<ReportAction> Sales => _actions.Where(action => action.Kind is ActionKind.Sell or ActionKind.Unpark);

    public void AttachTaxEstimate(TaxEstimate estimate, IReadOnlyDictionary<ReportActionId, (decimal Gain, decimal Tax)> perSale)
    {
        TaxEstimate = estimate;
        foreach (var action in Sales)
            if (perSale.TryGetValue(action.Id, out var sale))
                action.AttachTaxEstimate(sale.Gain, sale.Tax);
    }

    public bool IsOpen => Status is ReportStatus.New or ReportStatus.InProgress;

    public ReportAction? FindAction(ReportActionId actionId) => _actions.FirstOrDefault(action => action.Id == actionId);

    /// <summary>The action was done as planned or with the given numbers; it is complete.</summary>
    public Result<MarkedFill> MarkExecuted(ReportActionId actionId, decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note, DateTimeOffset now) =>
        RecordFill(actionId, quantity, price, fee, executedOn, note, final: true, now);

    /// <summary>Only part was done; can be called again for the rest. Reaching the planned quantity completes the action.</summary>
    public Result<MarkedFill> RecordPartialFill(ReportActionId actionId, decimal quantity, decimal price, decimal fee, DateOnly executedOn, string? note, DateTimeOffset now) =>
        RecordFill(actionId, quantity, price, fee, executedOn, note, final: false, now);

    /// <summary>Marks actions with operations already in the journal (see <see cref="JournalMatcher"/>).</summary>
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

    /// <summary>Takes a mark back. A closed report opens again; journal entries of the fills must be reversed.</summary>
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

    public void ApplyTransactionRecorded(FillReference reference, Guid transactionId) =>
        FindAction(reference.ActionId)?.ApplyRecorded(reference.FillNumber, transactionId);

    /// <summary>Portfolio refused a fill: the mark is rolled back so the user can correct it.</summary>
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

    /// <summary>Closes the report: actions nobody marked count as skipped ("закрито без відмітки").</summary>
    public Result Close(DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        foreach (var action in _actions)
            action.CloseUnmarked();
        Status = ReportStatus.Closed;
        ClosedAt = now;
        return Result.Success();
    }

    /// <summary>A newer report of the period replaces this one; marks already made stay in the journal.</summary>
    public Result SupersedeBy(ReportId newer, DateTimeOffset now)
    {
        if (!IsOpen)
            return ReportErrors.ReportIsNotOpen(Id.Value, Status);

        Status = ReportStatus.Superseded;
        SupersededBy = newer;
        ClosedAt = now;
        return Result.Success();
    }

    /// <summary>Contribution already paid in through this report (marked fills of the contribute action).</summary>
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

        StartWorkIfNew();
        CloseWhenAllActionsResolved(now);
        return new MarkedFill(action, fill.Value, new FillReference(Id, action.Id, fill.Value.Number));
    }

    private void StartWorkIfNew()
    {
        if (Status == ReportStatus.New)
            Status = ReportStatus.InProgress;
    }

    private void CloseWhenAllActionsResolved(DateTimeOffset now)
    {
        if (IsOpen && _actions.Count > 0 && _actions.All(action => action.IsResolved) && Status == ReportStatus.InProgress)
        {
            Status = ReportStatus.Closed;
            ClosedAt = now;
        }
    }
}

public static class ReportErrors
{
    public static Error NotFound(Guid reportId) => Error.NotFound("report.not_found", $"Report {reportId} does not exist.");

    public static Error ActionNotFound(Guid actionId) => Error.NotFound("report.action_not_found", $"The report has no action {actionId}.");

    public static Error ReportIsNotOpen(Guid reportId, ReportStatus status) =>
        Error.Conflict("report.not_open", $"Report {reportId} is {status}: actions can no longer be marked.");

    public static Error ActionAlreadyResolved(Guid actionId, ActionStatus status) =>
        Error.Conflict("report.action_resolved", $"Action {actionId} is already {status}; undo the mark first.");

    public static Error OnlyPendingCanBeSkipped(Guid actionId, ActionStatus status) =>
        Error.Conflict("report.action_not_pending", $"Action {actionId} is {status}: only a pending action can be skipped.");

    public static Error NothingToUndo(Guid actionId) => Error.Conflict("report.nothing_to_undo", $"Action {actionId} has no mark to undo.");

    public static Error PartialFillNotSupported(ActionKind kind) =>
        Error.Validation("report.partial_fill_not_supported", $"A {kind} action is marked as a whole; change the amount instead.");

    public static Error FillQuantityNotPositive() => Error.Validation("report.fill_quantity_not_positive", "Quantity must be above zero.");

    public static Error FillPriceNotPositive() => Error.Validation("report.fill_price_not_positive", "Price must be above zero.");

    public static Error FillFeeNegative() => Error.Validation("report.fill_fee_negative", "Fee cannot be negative.");

    public static Error NoActiveSubPortfolios(Guid accountId) =>
        Error.Validation("report.no_sub_portfolios", $"Account {accountId} has no active sub-portfolio with a strategy.");

    public static Error NoStrategyAnswered(string details) =>
        Error.Validation("report.no_strategy_answered", $"No strategy could evaluate the account (refresh quotes?): {details}");

    public static Error ContributionNegative() => Error.Validation("report.contribution_negative", "Contribution cannot be negative.");
}
