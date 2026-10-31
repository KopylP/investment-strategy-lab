using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Notifications.Delivery.Application.Shared;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class DismissClosedReport(Notifier notifier) : IIntegrationEventHandler<ReportClosedV1>
{
    public Task Handle(ReportClosedV1 closed, CancellationToken cancellationToken) =>
        notifier.DismissAsync(ReportNotification.TagOf(closed.ReportId), cancellationToken);
}
