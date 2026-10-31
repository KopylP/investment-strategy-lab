using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class ReportRequest : AggregateRoot<ReportRequestId>
{
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

    public decimal? Contribution { get; private set; }

    public DateOnly AsOf { get; private set; }

    public List<Guid> SubPortfolioIds { get; private set; }

    public ReportRequestStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset Deadline { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public ReportId? ReportId { get; private set; }
    public string? Error { get; private set; }

    public bool RunsPlanned { get; private set; }

    public Guid? SignalTriggerId { get; private set; }

    public string? Cause { get; private set; }

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

    private ReportRequest(ReportRequestId id) : base(id) => (Code, Title, SubPortfolioIds) = (null!, null!, null!);

    public static ReportRequest Create(
        Guid accountId, UserId ownerId, string code, string title, ReportKind kind, ReportTrigger trigger,
        decimal? contribution, DateOnly asOf, IEnumerable<Guid> subPortfolioIds, DateTimeOffset now, ReportCause? cause = null) =>
        new(ReportRequestId.New(), accountId, ownerId, code, title, kind, trigger, contribution, asOf, subPortfolioIds, now)
        {
            SignalTriggerId = cause?.SignalTriggerId,
            Cause = cause?.Text,
        };

    public bool IsPending => Status == ReportRequestStatus.Pending;

    public bool IsReady => IsPending && RunsPlanned && _runs.All(run => run.IsDone);

    public StrategyRun? RunOf(Guid subPortfolioId) => _runs.FirstOrDefault(run => run.SubPortfolioId == subPortfolioId);

    public void PlanRuns(IEnumerable<Guid> subPortfolioIds)
    {
        if (RunsPlanned)
            return;

        _runs.AddRange(subPortfolioIds.Distinct().Select(id => new StrategyRun(id)));
        RunsPlanned = true;
    }

    public void Submitted(Guid subPortfolioId, StrategyJob job) =>
        Replace(subPortfolioId, run => run with { Job = job, Submissions = run.Submissions + 1 });

    public void JobLost(Guid subPortfolioId) => Replace(subPortfolioId, run => run with { Job = null });

    public void Answered(Guid subPortfolioId, string answer) => Replace(subPortfolioId, run => run with { Answer = answer });

    public void RunFailed(Guid subPortfolioId, string errorCode, string errorMessage) =>
        Replace(subPortfolioId, run => run with { ErrorCode = errorCode, ErrorMessage = errorMessage });

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

        Raise(new ReportRequestFailed(this, now));
    }

    private void Replace(Guid subPortfolioId, Func<StrategyRun, StrategyRun> change)
    {
        var index = _runs.FindIndex(run => run.SubPortfolioId == subPortfolioId);
        if (index >= 0 && !_runs[index].IsDone)
            _runs[index] = change(_runs[index]);
    }
}
