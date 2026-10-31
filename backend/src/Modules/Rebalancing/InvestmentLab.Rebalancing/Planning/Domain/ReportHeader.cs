using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record ReportHeader(
    Guid AccountId,
    UserId OwnerId,
    string AccountName,
    Currency Currency,
    YearMonth Period,
    ReportKind Kind,
    ReportTrigger Trigger,
    DateOnly MarketAsOf,
    string Code,
    string Title)
{
    public static DateOnly MarketDateOf(IReadOnlyCollection<DateOnly> priceDates, DateOnly asOf) =>
        priceDates.Count > 0 ? priceDates.Max() : asOf;
}
