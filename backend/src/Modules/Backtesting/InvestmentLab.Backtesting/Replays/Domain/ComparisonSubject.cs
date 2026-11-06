using InvestmentLab.Backtesting.Experiments.Domain;

namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ComparisonSubject(ReplayResult Result, ExperimentConditions? Conditions = null)
{
    public ExperimentPeriod Period => Conditions?.Period ?? new ExperimentPeriod(Result.Start, Result.End);
    public TradingCosts Costs => Conditions?.Costs ?? new TradingCosts();
}
