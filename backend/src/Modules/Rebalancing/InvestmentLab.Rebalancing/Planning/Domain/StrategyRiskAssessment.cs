using InvestmentLab.Rebalancing.Reports.Domain;

namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyRiskAssessment(StrategyInfo Strategy, IReadOnlyList<RiskSignal> Signals, SectionRisk Section);
