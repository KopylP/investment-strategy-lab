using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

internal static class ReportRequestMapping
{
    public static ReportRequestDto ToDto(this ReportRequest request) => new(
        Id: request.Id.Value,
        AccountId: request.AccountId,
        Code: request.Code,
        Title: request.Title,
        Kind: request.Kind.ToString(),
        Trigger: request.Trigger.ToString(),
        Status: request.Status.ToString(),
        AsOf: request.AsOf,
        RequestedAt: request.RequestedAt,
        Deadline: request.Deadline,
        CompletedAt: request.CompletedAt,
        ReportId: request.ReportId?.Value,
        Error: request.Error,
        Strategies: request.RunsPlanned ? request.Runs.Count : request.SubPortfolioIds.Count,
        StrategiesAnswered: request.Runs.Count(run => run.IsDone));
}
