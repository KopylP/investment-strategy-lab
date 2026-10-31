using InvestmentLab.Rebalancing.Planning.Application.RequestReport;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.GetReportRequest;

public sealed class GetReportRequestHandler(IReportRequestRepository requests)
{
    public async Task<Result<ReportRequestDto>> Handle(Guid requestId, CancellationToken cancellationToken)
    {
        if (requestId == Guid.Empty || await requests.FindAsync(ReportRequestId.Of(requestId), cancellationToken) is not { } request)
            return Error.NotFound("report_request.not_found", $"Report request {requestId} does not exist.");

        return request.ToDto();
    }
}
