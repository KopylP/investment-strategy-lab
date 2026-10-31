namespace InvestmentLab.Rebalancing.Contracts;

public interface IReportScheduleQueries
{
    Task<DateTime?> NextAsync(Guid accountId, string code, CancellationToken cancellationToken);
}
