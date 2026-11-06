using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ResearchSource(ReplaySetup Setup, string Parameters, string TypeCode);
