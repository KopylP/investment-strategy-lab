using InvestmentLab.Performance.Valuation.Domain;

namespace InvestmentLab.Performance.Valuation.Application;

public sealed record AccountHistory(
    IReadOnlyList<Snapshot> Series,
    IReadOnlyList<Snapshot> SubPortfolios,
    IReadOnlyDictionary<DateOnly, PlanPoint>? Plan,
    IReadOnlyList<ReportMarkerDto> Reports);
