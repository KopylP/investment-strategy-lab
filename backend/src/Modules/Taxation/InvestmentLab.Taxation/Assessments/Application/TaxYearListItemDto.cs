using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxYearListItemDto(int Year, string PolicyCode, DateTimeOffset ComputedAt, TaxYearSummary Summary);
