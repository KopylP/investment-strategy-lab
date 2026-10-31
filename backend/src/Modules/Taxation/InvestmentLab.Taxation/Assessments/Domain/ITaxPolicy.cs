namespace InvestmentLab.Taxation.Assessments.Domain;

public interface ITaxPolicy
{
    string Code { get; }
    int Version { get; }

    string Jurisdiction { get; }
    bool IsTaxed { get; }
    string FxRule { get; }
    ILotMatcher LotMatcher { get; }
    ITaxRateSchedule RateSchedule { get; }
    ILossCarryforwardRule LossCarryforward { get; }
    IRoundingRule Rounding { get; }
}
