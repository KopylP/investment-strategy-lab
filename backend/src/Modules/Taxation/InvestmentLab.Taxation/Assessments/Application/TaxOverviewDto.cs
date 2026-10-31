namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxOverviewDto(IReadOnlyList<TaxYearListItemDto> Years, IReadOnlyList<TaxAccountDto> Accounts);
