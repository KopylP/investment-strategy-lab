namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class SignalReportRule : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public Guid SignalId { get; private set; }
    public Guid AccountId { get; private set; }
    public string ReportCode { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Problem { get; private set; }

    private SignalReportRule(Guid id) : base(id) => ReportCode = null!;

    private SignalReportRule(UserId ownerId, Guid signalId, Guid accountId, string reportCode, DateTimeOffset now) : base(Guid.CreateVersion7())
    {
        OwnerId = ownerId;
        SignalId = signalId;
        AccountId = accountId;
        ReportCode = reportCode;
        CreatedAt = now;
    }

    public static SignalReportRule Of(UserId ownerId, Guid signalId, Guid accountId, string reportCode, DateTimeOffset now) =>
        new(ownerId, signalId, accountId, reportCode, now);

    public void Acted() => Problem = null;

    public void CouldNotAct(string problem) => Problem = problem;
}
