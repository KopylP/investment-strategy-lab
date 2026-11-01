namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class Pl5Years50Percent : ILossCarryforwardRule
{
    public bool CarriesLosses => true;

    public decimal UsableIn(LossYear loss, int year) =>
        year > loss.Year && year <= loss.Year + 5 ? Math.Floor(loss.Amount / 2m * 100m) / 100m : 0m;
}
