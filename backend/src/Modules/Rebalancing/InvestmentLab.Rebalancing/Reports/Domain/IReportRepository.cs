namespace InvestmentLab.Rebalancing.Reports.Domain;

public interface IReportRepository
{
    Task<RebalancingReport?> FindAsync(ReportId reportId, CancellationToken cancellationToken);

    /// <param name="accountId">Null: every account the user can see.</param>
    Task<IReadOnlyList<RebalancingReport>> ListAsync(Guid? accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> ForPeriodAsync(Guid accountId, YearMonth period, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> OpenAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(RebalancingReport report);

    void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt) where TEvent : class;

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
