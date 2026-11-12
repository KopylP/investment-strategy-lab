namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxYearListItemDto(int Year, string PolicyCode, DateTimeOffset ComputedAt, TaxYearSummaryDto Summary);
