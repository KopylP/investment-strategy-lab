using System.Globalization;

namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class FlatRate(Percentage rate) : ITaxRateSchedule
{
    private const decimal PercentPerUnit = 100m;

    public string Label => string.Create(CultureInfo.InvariantCulture, $"{rate.Value * PercentPerUnit:0.##}%");

    public decimal TaxOn(decimal taxBase) => taxBase * rate.Value;
}
