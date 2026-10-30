namespace InvestmentLab.Rebalancing.Reports.Domain;

public interface IReportRepository
{
    Task<RebalancingReport?> FindAsync(ReportId reportId, CancellationToken cancellationToken);

    /// <param name="accountId">Null: every account the user can see.</param>
    Task<IReadOnlyList<RebalancingReport>> ListAsync(Guid? accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> ForPeriodAsync(Guid accountId, YearMonth period, CancellationToken cancellationToken);

    /// <summary>When the account's latest report of this code made on schedule was generated; null — never.</summary>
    Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> OpenAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(RebalancingReport report);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
