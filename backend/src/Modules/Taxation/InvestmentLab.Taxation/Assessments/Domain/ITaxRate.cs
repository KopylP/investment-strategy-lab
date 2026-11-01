namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxRate
{
    decimal Rate { get; }
    DateOnly EffectiveDate { get; }
}
