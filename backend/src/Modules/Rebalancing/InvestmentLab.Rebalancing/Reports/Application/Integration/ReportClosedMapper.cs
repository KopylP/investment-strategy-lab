using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class ReportClosedMapper : IDomainEventMapper<ReportClosed, ReportClosedV1>
{
    public ReportClosedV1 Map(ReportClosed closed) =>
        new(closed.ReportId.Value, closed.AccountId, closed.OwnerId.Value, closed.OccurredAt);
}
