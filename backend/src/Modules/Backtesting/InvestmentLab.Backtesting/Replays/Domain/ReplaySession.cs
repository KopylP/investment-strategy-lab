namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplaySession(
    DateOnly Date,
    IReadOnlyDictionary<Guid, decimal> Prices,
    FiredReports Signals,
    bool RebalanceScheduled,
    bool RiskReviewScheduled)
{
    public bool RebalanceRequested => RebalanceScheduled || Signals.Rebalance;
    public bool RiskReviewRequested => RiskReviewScheduled || Signals.RiskReview;
    public bool RebalanceBySignal => RebalanceRequested && !RebalanceScheduled;
    public bool RiskReviewBySignal => RiskReviewRequested && !RiskReviewScheduled;
    public IReadOnlyList<ReplaySignalState> SignalStates => Signals.States ?? [];

    public bool NeedsRiskAssessment(bool hasPositions) => RiskReviewRequested && hasPositions;
}
