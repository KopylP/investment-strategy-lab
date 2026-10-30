using InvestmentLab.MarketData.Contracts.Instruments;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reviews.Domain;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record ReportSummaryDto(
    Guid Id,
    Guid AccountId,
    string AccountName,
    string Period,
    string Kind,
    string Code,
    string Title,
    string Trigger,
    string Status,
    DateOnly MarketAsOf,
    DateTimeOffset GeneratedAt,
    DateTimeOffset? ClosedAt,
    string Currency,
    decimal Contribution,
    int ActionsTotal,
    int ActionsOpen,
    Guid? SupersededBy);

public sealed record ReportDetailsDto(
    ReportSummaryDto Summary,
    ContributionPlan ContributionPlan,
    IReadOnlyList<ReportActionDto> Actions,
    IReadOnlyList<ReportSection> Sections,
    IReadOnlyList<string> Warnings,
    PlanReview? Review,
    TaxEstimate? TaxEstimate,
    IReadOnlyDictionary<Guid, InstrumentLabelDto> Instruments,
    IReadOnlyDictionary<Guid, string> SubPortfolios);

public sealed record InstrumentLabelDto(string Symbol, string Name, string? Isin, string Currency);

public sealed record ReportActionDto(
    Guid Id,
    int Sequence,
    string Kind,
    Guid? InstrumentId,
    decimal PlannedQuantity,
    decimal PlannedPrice,
    decimal PlannedAmount,
    IReadOnlyList<ActionAllocation> Allocations,
    Guid? FromSubPortfolioId,
    Guid? ToSubPortfolioId,
    string Reason,
    string Status,
    decimal FilledQuantity,
    decimal RemainingQuantity,
    IReadOnlyList<Fill> Fills,
    string? SkipReason,
    string? Rejection,
    decimal? EstimatedGainPln,
    decimal? EstimatedTaxPln);

public sealed class ReportQueriesHandler(IReportRepository reports, IInstrumentCatalog instruments)
{
    private const string YahooPrefix = "yfinance:";

    public async Task<IReadOnlyList<ReportSummaryDto>> List(Guid? accountId, CancellationToken cancellationToken) =>
        [.. (await reports.ListAsync(accountId, cancellationToken)).Select(Summary)];

    public async Task<Result<ReportDetailsDto>> Get(Guid reportId, CancellationToken cancellationToken)
    {
        if (reportId == Guid.Empty || await reports.FindAsync(ReportId.Of(reportId), cancellationToken) is not { } report)
            return ReportErrors.NotFound(reportId);

        var catalog = (await instruments.GetInstruments(cancellationToken)).ToDictionary(instrument => instrument.Id);
        var used = report.Actions.Select(action => action.InstrumentId)
            .Concat(report.Sections.SelectMany(section => section.Orders.Select(order => (Guid?)order.InstrumentId)
                .Concat(section.Allocation.Select(row => row.InstrumentId))
                .Concat(section.Decision?.Scores.Select(score => (Guid?)score.InstrumentId) ?? [])
                .Concat(section.Risk?.Positions.Select(position => (Guid?)position.InstrumentId) ?? [])))
            .OfType<Guid>()
            .Distinct()
            .Where(catalog.ContainsKey)
            .ToDictionary(id => id, id => Label(catalog[id]));

        return new ReportDetailsDto(
            Summary(report),
            report.Contribution,
            [.. report.Actions.Select(action => new ReportActionDto(
                action.Id.Value, action.Sequence, action.Kind.ToString(), action.InstrumentId, action.PlannedQuantity, action.PlannedPrice,
                action.PlannedAmount, action.Allocations, action.FromSubPortfolioId, action.ToSubPortfolioId, action.Reason,
                action.Status.ToString(), action.FilledQuantity, action.RemainingQuantity, action.Fills, action.SkipReason, action.Rejection, action.EstimatedGainPln, action.EstimatedTaxPln))],
            report.Sections,
            report.Warnings,
            report.Review,
            report.TaxEstimate,
            used,
            report.Sections.ToDictionary(section => section.SubPortfolioId, section => section.Name));
    }

    private static InstrumentLabelDto Label(InstrumentDto instrument) => new(
        instrument.ProviderSymbols.FirstOrDefault(symbol => symbol.StartsWith(YahooPrefix, StringComparison.Ordinal)) is { } symbol
            ? symbol[YahooPrefix.Length..]
            : instrument.Name,
        instrument.Name,
        instrument.Isin,
        instrument.TradingCurrency);

    private static ReportSummaryDto Summary(RebalancingReport report) => new(
        report.Id.Value,
        report.AccountId,
        report.AccountName,
        report.Period.ToString(),
        report.Kind.ToString(),
        report.Code,
        report.Title,
        report.Trigger.ToString(),
        report.Status.ToString(),
        report.MarketAsOf,
        report.GeneratedAt,
        report.ClosedAt,
        report.Currency.Code,
        report.Contribution.Total,
        report.Actions.Count,
        report.Actions.Count(action => !action.IsResolved),
        report.SupersededBy?.Value);
}
