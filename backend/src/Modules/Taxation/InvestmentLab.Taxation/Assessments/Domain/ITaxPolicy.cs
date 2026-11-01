namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxPolicy
{
    string Code { get; }
    int Version { get; }

    string Jurisdiction { get; }
    bool IsTaxed { get; }
    string EstimateDisclaimer { get; }
    TaxPolicyTexts Texts { get; }
    IFxRule FxRule { get; }
    ILotMatcher LotMatcher { get; }
    ITaxableGainRule TaxableGain { get; }
    ITaxRateSchedule RateSchedule { get; }
    Percentage DividendRate { get; }
    ILossCarryforwardRule LossCarryforward { get; }
    IRoundingRule Rounding { get; }
}
