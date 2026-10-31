namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class PlWholeZloty : IRoundingRule
{
    public decimal Base(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
    public decimal Tax(decimal amount) => decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
}
