namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class BelkaTaxPolicy : ITaxPolicy
{
    public const string PolicyCode = "PL-BELKA";

    public string Code => PolicyCode;
    public string Jurisdiction => TaxPolicies.Poland;

    public int Version => 4;
    public bool IsTaxed => true;
    public string FxRule => "NBP, остання таблиця A перед днем угоди";
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Percentage.FromPercent(19m));
    public ILossCarryforwardRule LossCarryforward { get; } = new Pl5Years50Percent();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}
