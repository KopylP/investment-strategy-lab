namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxableGainRule
{
    decimal TaxableGainOf(Realization sale);
}
