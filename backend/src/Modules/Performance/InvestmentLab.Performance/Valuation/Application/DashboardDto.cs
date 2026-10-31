namespace InvestmentLab.Performance.Valuation.Application;

public sealed record DashboardDto(
    Guid? AccountId,
    string Currency,
    DateOnly? AsOf,
    string Period,
    decimal Value,
    decimal NetContributions,
    decimal Gain,
    decimal? Twr,
    decimal? TwrAnnualized,
    decimal? Xirr,
    decimal MaxDrawdown,
    decimal CurrentDrawdown,
    decimal? PlanValue,
    IReadOnlyList<SeriesPointDto> Series,
    IReadOnlyList<SubPortfolioPerformanceDto> SubPortfolios,
    IReadOnlyList<AccountPerformanceDto> Accounts,
    IReadOnlyList<ReportMarkerDto> Reports,
    decimal PositionsValue,
    decimal CostBasis);
