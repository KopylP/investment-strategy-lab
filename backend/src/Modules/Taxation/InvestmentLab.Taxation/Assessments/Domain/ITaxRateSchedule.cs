namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxRateSchedule
{
    string Label { get; }

    decimal TaxOn(decimal taxBase);
}
