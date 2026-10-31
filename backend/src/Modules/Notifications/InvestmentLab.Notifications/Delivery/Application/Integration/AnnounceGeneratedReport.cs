using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class AnnounceGeneratedReport(Notifier notifier, NextReportLookup nextReport) : IIntegrationEventHandler<ReportGeneratedV1>
{
    public async Task Handle(ReportGeneratedV1 generated, CancellationToken cancellationToken)
    {
        var next = await nextReport.FindAsync(generated.AccountId, generated.Code, cancellationToken);
        if (ReportNotification.For(generated, next) is { } notification)
            await notifier.SendAsync(notification, cancellationToken);
    }
}
