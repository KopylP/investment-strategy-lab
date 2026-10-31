using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.RequestReport;

public sealed record ReportRequestDto(
    Guid Id,
    Guid AccountId,
    string Code,
    string Title,
    string Kind,
    string Trigger,
    string Status,
    DateOnly AsOf,
    DateTimeOffset RequestedAt,
    DateTimeOffset Deadline,
    DateTimeOffset? CompletedAt,
    Guid? ReportId,
    string? Error,
    int Strategies,
    int StrategiesAnswered);

public sealed class ReportRequestQueriesHandler(IReportRequestRepository requests, TimeProvider clock)
{
    public static readonly TimeSpan RecentlyFinished = TimeSpan.FromDays(1);

    public async Task<IReadOnlyList<ReportRequestDto>> List(Guid? accountId, CancellationToken cancellationToken) =>
        [.. (await requests.RecentAsync(accountId, clock.GetUtcNow() - RecentlyFinished, cancellationToken)).Select(ToDto)];

    public async Task<Result<ReportRequestDto>> Get(Guid requestId, CancellationToken cancellationToken) =>
        requestId != Guid.Empty && await requests.FindAsync(ReportRequestId.Of(requestId), cancellationToken) is { } request
            ? ToDto(request)
            : Error.NotFound("report_request.not_found", $"Report request {requestId} does not exist.");

    private static ReportRequestDto ToDto(ReportRequest request) => new(
        request.Id.Value,
        request.AccountId,
        request.Code,
        request.Title,
        request.Kind.ToString(),
        request.Trigger.ToString(),
        request.Status.ToString(),
        request.AsOf,
        request.RequestedAt,
        request.Deadline,
        request.CompletedAt,
        request.ReportId?.Value,
        request.Error,
        request.RunsPlanned ? request.Runs.Count : request.SubPortfolioIds.Count,
        request.Runs.Count(run => run.IsDone));
}
