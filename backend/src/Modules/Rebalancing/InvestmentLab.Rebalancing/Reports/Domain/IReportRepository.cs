namespace InvestmentLab.Rebalancing.Reports.Domain;

public interface IReportRepository
{
    Task<RebalancingReport?> FindAsync(ReportId reportId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> ListAsync(Guid? accountId, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> ForPeriodAsync(Guid accountId, YearMonth period, CancellationToken cancellationToken);

    Task<DateTimeOffset?> LastScheduledAsync(Guid accountId, string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<RebalancingReport>> OpenAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(RebalancingReport report);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
