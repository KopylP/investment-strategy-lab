using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.Integration;

internal sealed class ReportSupersededMapper : IDomainEventMapper<ReportSuperseded, ReportSupersededV1>
{
    public ReportSupersededV1 Map(ReportSuperseded superseded) =>
        new(superseded.ReportId.Value, superseded.AccountId, superseded.SupersededBy.Value, superseded.OccurredAt);
}
