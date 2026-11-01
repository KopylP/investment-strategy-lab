namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ILossCarryforwardRule
{
    bool CarriesLosses { get; }

    decimal UsableIn(LossYear loss, int year);
}
