using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReportOnDemand;

public sealed class RequestReportOnDemandHandler(RequestReportHandler requests)
{
    public async Task<Result<Guid>> Handle(RequestReportCommand command, CancellationToken cancellationToken)
    {
        var requested = await requests.Handle(command, ReportTrigger.OnDemand, cancellationToken);
        if (requested.IsFailure)
            return requested.Error;

        return requested.Value.Value;
    }
}
