using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReportOnDemand;

public sealed class RequestReportOnDemandHandler(RequestReportHandler requests)
{
    public Task<Result<Guid>> Handle(RequestReportCommand command, CancellationToken cancellationToken) =>
        requests.Handle(command, nameof(ReportTrigger.OnDemand), cancellationToken);
}
