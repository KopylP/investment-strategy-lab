using InvestmentLab.Rebalancing.Contracts;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.AdvanceReportRequests;

internal static class ReportRequestEventMapping
{
    extension(ReportRequest request)
    {
        public ReportRequestFailedV1 ToFailedEvent() => new(
            request.Id.Value,
            request.AccountId,
            request.OwnerId.Value,
            request.Code,
            request.Title,
            request.Kind.ToString(),
            request.Trigger.ToString(),
            YearMonth.Containing(request.AsOf).ToString(),
            request.Error ?? "",
            request.RequestedAt,
            request.CompletedAt ?? request.RequestedAt);
    }
}
