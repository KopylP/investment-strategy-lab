namespace InvestmentLab.Taxation.Assessments.Application.Shared;

public sealed record TaxYearDetailsDto(
    int Year,
    string PolicyCode,
    string FxRule,
    DateTimeOffset ComputedAt,
    TaxYearSummaryDto Summary,
    IReadOnlyList<RealizationDto> Realizations,
    IReadOnlyList<DividendIncomeDto> Dividends,
    IReadOnlyList<CarryforwardUseDto> CarryforwardUses,
    IReadOnlyList<OpenLotSnapshotDto> OpenLots,
    IReadOnlyDictionary<Guid, string> Instruments,
    IReadOnlyList<TaxAccountDto> Accounts,
    IReadOnlyList<string> Warnings,
    bool IsTaxed,
    string RateLabel);
