namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class Backtest : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public Guid StrategyId { get; private set; }
    public int StrategyVersion { get; private set; }
    public BacktestStatus Status { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public ReplayResult? Result { get; private set; }

    private Backtest(Guid id) : base(id) { }

    public static Backtest Request(UserId ownerId, Guid strategyId, int strategyVersion, DateTimeOffset now) =>
        new(Guid.CreateVersion7())
        {
            OwnerId = ownerId,
            StrategyId = strategyId,
            StrategyVersion = strategyVersion,
            Status = BacktestStatus.Queued,
            RequestedAt = now,
        };

    public bool IsWaitingOrRunning => Status is BacktestStatus.Queued or BacktestStatus.Running;

    public bool IsOlderThan(TimeSpan age, DateTimeOffset now) =>
        Status == BacktestStatus.Succeeded && FinishedAt is { } finished && now - finished > age;

    public void RequestAgain(DateTimeOffset now)
    {
        if (IsWaitingOrRunning || Status == BacktestStatus.NotSupported)
            return;

        Status = BacktestStatus.Queued;
        RequestedAt = now;
    }

    public void Start() => Status = BacktestStatus.Running;

    public void Complete(ReplayResult result, DateTimeOffset now)
    {
        Result = result;
        Status = BacktestStatus.Succeeded;
        FailureReason = null;
        FinishedAt = now;
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        Status = BacktestStatus.Failed;
        FailureReason = reason;
        FinishedAt = now;
    }

    public void MarkNotSupported(DateTimeOffset now)
    {
        Status = BacktestStatus.NotSupported;
        FinishedAt = now;
    }
}
