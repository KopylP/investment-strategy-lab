namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxCurrencyRate
{
    decimal Rate { get; }
    DateOnly EffectiveDate { get; }
}
