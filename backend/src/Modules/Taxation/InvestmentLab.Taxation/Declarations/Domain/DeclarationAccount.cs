using InvestmentLab.Taxation.Assessments.Domain;

namespace InvestmentLab.Taxation.Declarations.Domain;

public sealed record DeclarationAccount(
    string Name,
    string Broker,
    ITaxPolicy Policy,
    IReadOnlyList<TaxYearSummary> Years,
    IReadOnlyList<Realization> Sales,
    IReadOnlyList<DividendIncome> Dividends,

    Guid AccountId = default, IReadOnlyDictionary<int, Pit8CAmounts>? Pit8C = null)
{
    public TaxYearSummary? In(int year) => Years.FirstOrDefault(summary => summary.Year == year);

    public Pit8CAmounts? Pit8COf(int year) => Pit8C?.GetValueOrDefault(year);
}
