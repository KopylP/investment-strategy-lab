namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class SignalReportRule : AggregateRoot<Guid>, IUserOwned
{
    public UserId OwnerId { get; private set; }
    public Guid SignalId { get; private set; }
    public Guid AccountId { get; private set; }
    public string ReportCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Problem { get; private set; }
    public bool FromStrategy { get; private set; }
    public DateTimeOffset? DismissedAt { get; private set; }

    public bool IsDismissed => DismissedAt is not null;

    private SignalReportRule(Guid id) : base(id) => ReportCode = null!;

    private SignalReportRule(UserId ownerId, Guid signalId, Guid accountId, string reportCode, DateTimeOffset now, bool fromStrategy) : base(Guid.CreateVersion7())
    {
        OwnerId = ownerId;
        SignalId = signalId;
        AccountId = accountId;
        ReportCode = reportCode;
        CreatedAt = now;
        FromStrategy = fromStrategy;
    }

    public static SignalReportRule Of(UserId ownerId, Guid signalId, Guid accountId, string reportCode, DateTimeOffset now) =>
        new(ownerId, signalId, accountId, reportCode, now, fromStrategy: false);

    public static SignalReportRule Declared(DeclaredSignalRule declared, DateTimeOffset now) =>
        new(declared.OwnerId, declared.SignalId, declared.AccountId, declared.ReportCode, now, fromStrategy: true);

    public Result Dismiss(DateTimeOffset now)
    {
        if (IsDismissed)
            return SignalReportRuleErrors.NotFound();

        DismissedAt = now;

        return Result.Success();
    }

    public Result Restore()
    {
        if (!IsDismissed)
            return SignalReportRuleErrors.AlreadyExists();

        DismissedAt = null;
        FromStrategy = false;
        Problem = null;

        return Result.Success();
    }

    public void Acted() => Problem = null;

    public void CouldNotAct(string problem) => Problem = problem;
}
