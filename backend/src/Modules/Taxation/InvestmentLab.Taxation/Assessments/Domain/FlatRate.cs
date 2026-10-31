namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class FlatRate(Percentage rate) : ITaxRateSchedule
{
    public Percentage Rate => rate;
}
