namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class NoCarryforward : ILossCarryforwardRule
{
    public bool CarriesLosses => false;

    public decimal UsableIn(LossYear loss, int year) => 0m;
}
