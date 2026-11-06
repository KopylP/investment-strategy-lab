namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed class ResearchExperiment : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public Guid StrategyId { get; private set; }
    public int StrategyVersion { get; private set; }
    public ExperimentConditions Conditions { get; private set; } = null!;
    public ExperimentStatus Status { get; private set; } = ExperimentStatus.Queued;
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public string? FailureReason { get; private set; }
    public ResearchReport? Result { get; private set; }

    private ResearchExperiment(Guid id) : base(id) { }

    public static Result<ResearchExperiment> Request(
        UserId owner,
        Guid strategyId,
        int version,
        ExperimentConditions conditions,
        DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (conditions.Problem(today) is { } problem)
            return problem;

        return new ResearchExperiment(Guid.CreateVersion7())
        {
            OwnerId = owner,
            StrategyId = strategyId,
            StrategyVersion = version,
            Conditions = conditions,
            RequestedAt = now,
        };
    }

    public void Start()
    {
        Status = ExperimentStatus.Running;
    }

    public void Complete(ResearchReport report, DateTimeOffset now)
    {
        Result = report;
        Status = ExperimentStatus.Succeeded;
        FinishedAt = now;
        FailureReason = null;
    }

    public void Fail(string reason, DateTimeOffset now)
    {
        Status = ExperimentStatus.Failed;
        FailureReason = reason;
        FinishedAt = now;
    }
}
