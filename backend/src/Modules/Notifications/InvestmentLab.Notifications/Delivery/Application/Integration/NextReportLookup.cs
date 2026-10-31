using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class NextReportLookup(IReportScheduleQueries schedule)
{
    public async Task<NextReport?> FindAsync(Guid accountId, string? code, CancellationToken cancellationToken)
    {
        if (code is null)
            return null;

        var next = await schedule.NextAsync(accountId, code, cancellationToken);

        return new NextReport(next);
    }
}
