using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public readonly record struct ReportRequestId
{
    public Guid Value { get; }

    private ReportRequestId(Guid value) => Value = value;

    public static ReportRequestId New() => new(Guid.CreateVersion7());

    public static ReportRequestId Of(Guid value) =>
        value != Guid.Empty ? new(value) : throw new ArgumentException("Empty report request id.", nameof(value));

    public override string ToString() => Value.ToString();
}

public enum ReportRequestStatus
{
    /// <summary>The strategies are still thinking; the user sees «Готується».</summary>
    Pending,

    /// <summary>The report is made (<see cref="ReportRequest.ReportId"/>) and published to the user.</summary>
    Completed,

    /// <summary>No report could be made (<see cref="ReportRequest.Error"/>).</summary>
    Failed,
}

/// <summary>A background run of one strategy in the engine: what to poll, and the strategy version it answers for.</summary>
/// <param name="Task">"Decide" or "AssessRisk".</param>
public sealed record StrategyJob(string JobId, string Task, Guid StrategyId, int StrategyVersion, Guid? CashInstrumentId);

/// <summary>One sub-portfolio's strategy for a requested report: submitted, then answered (the answer as JSON) or failed.</summary>
public sealed record StrategyRun(
    Guid SubPortfolioId, StrategyJob? Job = null, string? Answer = null, string? ErrorCode = null, string? ErrorMessage = null, int Submissions = 0)
{
    public bool IsDone => Answer is not null || ErrorCode is not null;
}

/// <summary>
/// A report asked for (on schedule or on demand) that is made once every strategy has answered: strategies may think
/// for minutes or hours (an AI agent), so the request waits in the background — Pending → Completed with the report
/// published, or Failed. Strategies still silent at <see cref="Deadline"/> count as not answered.
/// </summary>
public sealed class ReportRequest : AggregateRoot<ReportRequestId>
{
    /// <summary>How long strategies may think before the report is made without them.</summary>
    public static readonly TimeSpan Patience = TimeSpan.FromHours(6);

    public const string TimedOut = "report_request.timed_out";

    public const int MaxErrorLength = 2000;

    private readonly List<StrategyRun> _runs = [];

    public Guid AccountId { get; private set; }
    public UserId OwnerId { get; private set; }
    public string Code { get; private set; }
    public string Title { get; private set; }
    public ReportKind Kind { get; private set; }
    public ReportTrigger Trigger { get; private set; }

    /// <summary>Rebalance reports only; null — the scope's part of the plan's monthly contribution.</summary>
    public decimal? Contribution { get; private set; }

    public DateOnly AsOf { get; private set; }

    /// <summary>The sub-portfolios the report covers (those whose strategy makes it, or the ones asked for).</summary>
    public List<Guid> SubPortfolioIds { get; private set; }

    public ReportRequestStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset Deadline { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public ReportId? ReportId { get; private set; }
    public string? Error { get; private set; }

    /// <summary>False until it is known which sub-portfolios' strategies have to answer (a risk review skips empty ones).</summary>
    public bool RunsPlanned { get; private set; }

    public IReadOnlyList<StrategyRun> Runs => _runs;

    private ReportRequest(
        ReportRequestId id, Guid accountId, UserId ownerId, string code, string title, ReportKind kind, ReportTrigger trigger,
        decimal? contribution, DateOnly asOf, IEnumerable<Guid> subPortfolioIds, DateTimeOffset now) : base(id)
    {
        AccountId = accountId;
        OwnerId = ownerId;
        Code = code;
        Title = title;
        Kind = kind;
        Trigger = trigger;
        Contribution = contribution;
        AsOf = asOf;
        SubPortfolioIds = [.. subPortfolioIds];
        Status = ReportRequestStatus.Pending;
        RequestedAt = now;
        Deadline = now + Patience;
    }

    // EF Core.
    private ReportRequest(ReportRequestId id) : base(id) => (Code, Title, SubPortfolioIds) = (null!, null!, null!);

    public static ReportRequest Create(
        Guid accountId, UserId ownerId, string code, string title, ReportKind kind, ReportTrigger trigger,
        decimal? contribution, DateOnly asOf, IEnumerable<Guid> subPortfolioIds, DateTimeOffset now) =>
        new(ReportRequestId.New(), accountId, ownerId, code, title, kind, trigger, contribution, asOf, subPortfolioIds, now);

    public bool IsPending => Status == ReportRequestStatus.Pending;

    /// <summary>Every strategy that has to answer has answered (or failed): the report can be made.</summary>
    public bool IsReady => IsPending && RunsPlanned && _runs.All(run => run.IsDone);

    public StrategyRun? RunOf(Guid subPortfolioId) => _runs.FirstOrDefault(run => run.SubPortfolioId == subPortfolioId);

    /// <summary>These sub-portfolios' strategies have to answer before the report is made.</summary>
    public void PlanRuns(IEnumerable<Guid> subPortfolioIds)
    {
        if (RunsPlanned)
            return;
        _runs.AddRange(subPortfolioIds.Distinct().Select(id => new StrategyRun(id)));
        RunsPlanned = true;
    }

    public void Submitted(Guid subPortfolioId, StrategyJob job) =>
        Replace(subPortfolioId, run => run with { Job = job, Submissions = run.Submissions + 1 });

    /// <summary>The engine no longer knows the job (it restarted): it is submitted again.</summary>
    public void JobLost(Guid subPortfolioId) => Replace(subPortfolioId, run => run with { Job = null });

    public void Answered(Guid subPortfolioId, string answer) => Replace(subPortfolioId, run => run with { Answer = answer });

    public void RunFailed(Guid subPortfolioId, string errorCode, string errorMessage) =>
        Replace(subPortfolioId, run => run with { ErrorCode = errorCode, ErrorMessage = errorMessage });

    /// <summary>Past the deadline, strategies still thinking count as not answered; true when that happened now.</summary>
    public bool TimeOutWhenDue(DateTimeOffset now)
    {
        if (!IsPending || now < Deadline)
            return false;

        PlanRuns([]);
        foreach (var run in _runs.Where(run => !run.IsDone).ToList())
            RunFailed(run.SubPortfolioId, TimedOut, $"стратегія не відповіла за {Patience.TotalHours:0} год");
        return true;
    }

    public void Complete(ReportId reportId, DateTimeOffset now)
    {
        if (!IsPending)
            return;
        Status = ReportRequestStatus.Completed;
        ReportId = reportId;
        CompletedAt = now;
    }

    public void Fail(string error, DateTimeOffset now)
    {
        if (!IsPending)
            return;
        Status = ReportRequestStatus.Failed;
        Error = error.Length > MaxErrorLength ? error[..MaxErrorLength] : error;
        CompletedAt = now;
    }

    private void Replace(Guid subPortfolioId, Func<StrategyRun, StrategyRun> change)
    {
        var index = _runs.FindIndex(run => run.SubPortfolioId == subPortfolioId);
        if (index >= 0 && !_runs[index].IsDone)
            _runs[index] = change(_runs[index]);
    }
}

public interface IReportRequestRepository
{
    Task<ReportRequest?> FindAsync(ReportRequestId requestId, CancellationToken cancellationToken);

    /// <summary>Every request still waiting for its strategies (all users': the background job's view).</summary>
    Task<IReadOnlyList<ReportRequest>> PendingAsync(CancellationToken cancellationToken);

    /// <summary>The account's request of this code still waiting, if any.</summary>
    Task<ReportRequest?> PendingAsync(Guid accountId, string code, CancellationToken cancellationToken);

    /// <summary>When the account's latest request of this code was made on schedule; null — never.</summary>
    Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken);

    /// <summary>Requests still pending, and those finished since <paramref name="since"/>, newest first.</summary>
    /// <param name="accountId">Null: every account the user can see.</param>
    Task<IReadOnlyList<ReportRequest>> RecentAsync(Guid? accountId, DateTimeOffset since, CancellationToken cancellationToken);

    void Add(ReportRequest request);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
