using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxProgressDto(
    int Year, decimal TaxDue, decimal Gain, decimal Dividends, int TaxedAccounts, IReadOnlyList<TaxTimelinePoint> Points, decimal? AlertShare,
    IReadOnlyList<string> Incomplete);
