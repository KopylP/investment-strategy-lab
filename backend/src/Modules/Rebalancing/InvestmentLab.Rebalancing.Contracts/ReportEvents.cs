namespace InvestmentLab.Rebalancing.Contracts;

/// <summary>A report was generated; its planned actions feed the "everything as planned" shadow portfolio.</summary>
/// <param name="Kind">Monthly, Recalculation or RiskReview.</param>
/// <param name="Trigger">Scheduled or OnDemand; null in events stored before it was added.</param>
/// <param name="Title">The report's name as its strategy declares it; null in events stored before it was added.</param>
/// <param name="RequestedAt">When it was asked for (its strategies thought until <paramref name="GeneratedAt"/>); null in events stored before it was added.</param>
/// <param name="Code">Which of the strategies' reports, e.g. "rebalance"; null in events stored before it was added.</param>
public sealed record ReportGeneratedV1(
    Guid ReportId,
    Guid AccountId,
    Guid OwnerId,
    string Period,
    string Kind,
    DateOnly MarketAsOf,
    string Currency,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<PlannedActionDto> Actions,
    string? AccountName = null,
    string? Trigger = null,
    string? Title = null,
    DateTimeOffset? RequestedAt = null,
    string? Code = null);

/// <summary>A report asked for could not be made: no strategy answered, or its deadline passed.</summary>
/// <param name="Code">Which of the strategies' reports, e.g. "rebalance".</param>
/// <param name="Kind">Monthly or RiskReview.</param>
/// <param name="Trigger">Scheduled or OnDemand.</param>
/// <param name="Period">yyyy-MM of the market date it was for.</param>
public sealed record ReportRequestFailedV1(
    Guid RequestId,
    Guid AccountId,
    Guid OwnerId,
    string Code,
    string Title,
    string Kind,
    string Trigger,
    string Period,
    string Error,
    DateTimeOffset RequestedAt,
    DateTimeOffset FailedAt);

/// <param name="Kind">Contribute, Sell, Unpark, Transfer, Buy or Park. For Contribute the quantity is the amount and the price is 1.</param>
public sealed record PlannedActionDto(
    Guid ActionId,
    string Kind,
    Guid? InstrumentId,
    decimal Quantity,
    decimal Price,
    decimal Amount,
    IReadOnlyList<PlannedAllocationDto> Allocations,
    Guid? FromSubPortfolioId);

public sealed record PlannedAllocationDto(Guid SubPortfolioId, decimal Quantity);

public sealed record ReportClosedV1(Guid ReportId, Guid AccountId, Guid OwnerId, DateTimeOffset ClosedAt);

public sealed record ReportSupersededV1(Guid ReportId, Guid AccountId, Guid SupersededBy, DateTimeOffset SupersededAt);

/// <summary>Open reports of an account, for badges and the dashboard.</summary>
public interface IReportQueries
{
    Task<int> GetOpenActionsCount(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>When an account's reports come next, so whoever tells about one can say when to expect the next.</summary>
public interface IReportScheduleQueries
{
    /// <summary>
    /// Local date and time (in the service's time zone) the report with this code next comes on schedule; null when it
    /// does not: off, made only on demand, automatic generation turned off, or no strategy of the account makes it.
    /// </summary>
    Task<DateTime?> NextAsync(Guid accountId, string code, CancellationToken cancellationToken);
}
