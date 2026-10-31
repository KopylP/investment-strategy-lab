using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class AnnounceFailedReport(Notifier notifier, NextReportLookup nextReport) : IIntegrationEventHandler<ReportRequestFailedV1>
{
    public async Task Handle(ReportRequestFailedV1 failed, CancellationToken cancellationToken)
    {
        var next = await nextReport.FindAsync(failed.AccountId, failed.Code, cancellationToken);
        if (ReportNotification.For(failed, next) is { } notification)
            await notifier.SendAsync(notification, cancellationToken);
    }
}
