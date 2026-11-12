using InvestmentLab.SharedKernel;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Application.Shared;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.ListReportRequests;

public sealed class ListReportRequestsHandler(IReportRequestRepository requests, TimeProvider clock) : IHandler
{
    private static readonly TimeSpan RecentlyFinished = TimeSpan.FromDays(1);

    public async Task<IReadOnlyList<ReportRequestDto>> Handle(Guid? accountId, CancellationToken cancellationToken)
    {
        var finishedSince = clock.GetUtcNow() - RecentlyFinished;
        var recent = await requests.RecentAsync(accountId, finishedSince, cancellationToken);

        return [.. recent.Select(request => request.ToDto())];
    }
}
