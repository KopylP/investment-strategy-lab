using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Contracts;

namespace InvestmentLab.Notifications.Delivery.Application.Integration;

internal sealed class DismissSupersededReport(Notifier notifier) : IIntegrationEventHandler<ReportSupersededV1>
{
    public Task Handle(ReportSupersededV1 superseded, CancellationToken cancellationToken) =>
        notifier.DismissAsync(ReportNotification.TagOf(superseded.ReportId), cancellationToken);
}
