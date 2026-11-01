using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Rebalancing.Planning.Application.AdvanceReportRequests;

public sealed class ReportRequestsJob(
    IReportRequestRepository requests, IServiceScopeFactory scopes, TimeProvider clock, ILogger<ReportRequestsJob> logger) : IScheduledJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var pending in await requests.PendingAsync(cancellationToken))
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var request = await scope.ServiceProvider.GetRequiredService<IReportRequestRepository>().FindAsync(pending.Id, cancellationToken);
                if (request is not null)
                    await scope.ServiceProvider.GetRequiredService<ReportRequestProcessor>().AdvanceAsync(request, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(
                    exception,
                    "Report request {RequestId} ({Code}) of account {AccountId} could not be moved on",
                    pending.Id,
                    pending.Code,
                    pending.AccountId);
                await GiveUpWhenDueAsync(pending.Id, exception, cancellationToken);
            }
        }
    }

    private async Task GiveUpWhenDueAsync(ReportRequestId requestId, Exception exception, CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IReportRequestRepository>();
        if (await repository.FindAsync(requestId, cancellationToken) is not { } request || clock.GetUtcNow() < request.Deadline)
            return;

        request.Fail(Message.Of("report.failed", new { error = exception.Message }), clock.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
    }
}
