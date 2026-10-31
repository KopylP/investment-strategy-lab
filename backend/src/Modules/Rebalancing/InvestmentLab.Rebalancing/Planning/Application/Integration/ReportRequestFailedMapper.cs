using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Integration;

internal sealed class ReportRequestFailedMapper : IDomainEventMapper<ReportRequestFailed, ReportRequestFailedV1>
{
    public ReportRequestFailedV1 Map(ReportRequestFailed failed) => failed.Request.ToFailedEvent();
}
