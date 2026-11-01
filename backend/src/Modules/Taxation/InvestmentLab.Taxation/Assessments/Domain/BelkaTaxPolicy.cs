namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class BelkaTaxPolicy : ITaxPolicy
{
    public const string PolicyCode = "PL-BELKA";

    public static readonly Percentage Rate = Percentage.FromPercent(19m);

    public string Code => PolicyCode;
    public string Jurisdiction => TaxPolicies.Poland;

    public int Version => 4;
    public bool IsTaxed => true;
    public string EstimateDisclaimer => Message.Of("tax.policy.pl_belka.disclaimer");
    public IFxRule FxRule { get; } = new NbpPreviousBusinessDay();
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Rate);
    public Percentage DividendRate => Rate;
    public ILossCarryforwardRule LossCarryforward { get; } = new Pl5Years50Percent();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}
