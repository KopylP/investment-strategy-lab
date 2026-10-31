namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxRateSchedule
{
    Percentage Rate { get; }
}
