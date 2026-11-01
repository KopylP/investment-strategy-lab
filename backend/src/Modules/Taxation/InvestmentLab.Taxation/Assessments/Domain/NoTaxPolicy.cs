namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class NoTaxPolicy(string code) : ITaxPolicy
{
    public string Code => code;
    public string Jurisdiction => TaxPolicies.Poland;
    public int Version => 1;
    public bool IsTaxed => false;
    public string EstimateDisclaimer => "";
    public TaxPolicyTexts Texts { get; } = new(
        Name: Message.Of($"tax.policy.{code.Replace('-', '_').ToLowerInvariant()}.name"),
        Hint: Message.Of($"tax.policy.{code.Replace('-', '_').ToLowerInvariant()}.hint"));
    public IFxRule FxRule { get; } = new NbpPreviousBusinessDay();
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Percentage.Zero);
    public Percentage DividendRate => Percentage.Zero;
    public ILossCarryforwardRule LossCarryforward { get; } = new NoCarryforward();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}
