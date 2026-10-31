namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed record TaxComputation(IReadOnlyList<TaxYearResult> Years, IReadOnlyList<OpenLotSnapshot> OpenLots, IReadOnlyList<string> Warnings)
{
    public TaxYearResult? Year(int year) => Years.FirstOrDefault(result => result.Summary.Year == year);
}
