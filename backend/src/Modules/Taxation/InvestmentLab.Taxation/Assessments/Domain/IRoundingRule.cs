namespace InvestmentLab.Taxation.Assessments.Domain;

public interface IRoundingRule
{
    decimal Base(decimal amount);
    decimal Tax(decimal amount);
}
