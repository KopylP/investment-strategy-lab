namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxOverviewDto(IReadOnlyList<TaxYearListItemDto> Years, IReadOnlyList<TaxAccountDto> Accounts);
