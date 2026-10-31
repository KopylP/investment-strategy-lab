namespace InvestmentLab.Rebalancing.Planning.Domain;

public interface IReportRequestRepository
{
    Task<ReportRequest?> FindAsync(ReportRequestId requestId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportRequest>> PendingAsync(CancellationToken cancellationToken);

    Task<ReportRequest?> PendingAsync(Guid accountId, string code, CancellationToken cancellationToken);

    Task<bool> AnsweredSignalAsync(Guid signalTriggerId, Guid accountId, string code, CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReportRequest>> RecentAsync(Guid? accountId, DateTimeOffset since, CancellationToken cancellationToken);

    void Add(ReportRequest request);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
