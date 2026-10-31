using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Assessments.Application;

public sealed record TaxYearDetailsDto(
    int Year,
    string PolicyCode,
    string FxRule,
    DateTimeOffset ComputedAt,
    TaxYearSummary Summary,
    IReadOnlyList<Realization> Realizations,
    IReadOnlyList<DividendIncome> Dividends,
    IReadOnlyList<CarryforwardUse> CarryforwardUses,
    IReadOnlyList<OpenLotSnapshot> OpenLots,
    IReadOnlyDictionary<Guid, string> Instruments,
    IReadOnlyList<TaxAccountDto> Accounts,
    IReadOnlyList<string> Warnings);
