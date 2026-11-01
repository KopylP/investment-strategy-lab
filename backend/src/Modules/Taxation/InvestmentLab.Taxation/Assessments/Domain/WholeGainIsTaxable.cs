namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class WholeGainIsTaxable : ITaxableGainRule
{
    public decimal TaxableGainOf(Realization sale) => sale.GainInTaxCurrency;
}
