namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxSituation(IReadOnlyList<TaxEvent> Events, IReadOnlyList<LossYear> Losses, ITaxPolicy Policy, int Year)
{
    public TaxYearResult? YearWith(IReadOnlyList<TaxEvent> proposedSales)
    {
        var computation = TaxCalculator.Compute([.. Events, .. proposedSales], Policy, Losses);

        return computation.Year(Year);
    }
}
