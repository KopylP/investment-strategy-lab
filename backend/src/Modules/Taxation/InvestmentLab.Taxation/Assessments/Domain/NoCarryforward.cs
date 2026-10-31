namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class NoCarryforward : ILossCarryforwardRule
{
    public decimal UsableIn(LossYear loss, int year) => 0m;
}
