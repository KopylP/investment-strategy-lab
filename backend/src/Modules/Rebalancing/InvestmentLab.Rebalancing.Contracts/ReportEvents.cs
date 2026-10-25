namespace InvestmentLab.Rebalancing.Contracts;

/// <summary>A report was generated; its planned actions feed the "everything as planned" shadow portfolio.</summary>
/// <param name="Kind">Monthly, Recalculation or RiskReview.</param>
public sealed record ReportGeneratedV1(
    Guid ReportId,
    Guid AccountId,
    Guid OwnerId,
    string Period,
    string Kind,
    DateOnly MarketAsOf,
    string Currency,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<PlannedActionDto> Actions);

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
