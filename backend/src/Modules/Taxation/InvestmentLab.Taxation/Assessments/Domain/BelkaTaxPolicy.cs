namespace InvestmentLab.Taxation.Assessments.Domain;

public sealed class BelkaTaxPolicy : ITaxPolicy
{
    public const string PolicyCode = "PL-BELKA";

    public static readonly Percentage Rate = Percentage.FromPercent(19m);

    public string Code => PolicyCode;
    public string Jurisdiction => TaxPolicies.Poland;

    public int Version => 5;
    public bool IsTaxed => true;
    public string EstimateDisclaimer => Message.Of("tax.policy.pl_belka.disclaimer");
    public TaxPolicyTexts Texts { get; } = new(
        Name: Message.Of("tax.policy.pl_belka.name"),
        Hint: Message.Of("tax.policy.pl_belka.hint"),
        Summary: Message.Of("tax.policy.pl_belka.summary"),
        EstimateNote: Message.Of("tax.policy.pl_belka.estimate_note"),
        TaxedAccountsLabel: Message.Of("tax.policy.pl_belka.taxed_accounts"),
        OverviewHint: Message.Of("tax.policy.pl_belka.overview_hint"),
        ProgressNote: Message.Of("tax.policy.pl_belka.progress_note"),
        LossesNote: Message.Of("tax.policy.pl_belka.losses_note"));
    public IFxRule FxRule { get; } = new NbpPreviousBusinessDay();
    public ILotMatcher LotMatcher { get; } = new FifoLotMatcher();
    public ITaxableGainRule TaxableGain { get; } = new WholeGainIsTaxable();
    public ITaxRateSchedule RateSchedule { get; } = new FlatRate(Rate);
    public Percentage DividendRate => Rate;
    public ILossCarryforwardRule LossCarryforward { get; } = new Pl5Years50Percent();
    public IRoundingRule Rounding { get; } = new PlWholeZloty();
}
