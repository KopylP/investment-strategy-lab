using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxYearListItemDto(int Year, string PolicyCode, DateTimeOffset ComputedAt, TaxYearSummary Summary);
