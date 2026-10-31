namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ILossCarryforwardRule
{
    decimal UsableIn(LossYear loss, int year);
}
