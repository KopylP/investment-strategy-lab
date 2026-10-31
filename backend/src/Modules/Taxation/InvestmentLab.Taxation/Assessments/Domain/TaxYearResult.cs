namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxYearResult(
    TaxYearSummary Summary,
    IReadOnlyList<Realization> Realizations,
    IReadOnlyList<DividendIncome> Dividends,
    IReadOnlyList<CarryforwardUse> CarryforwardUses);
