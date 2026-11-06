using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record TrainingResult(ParameterVariant Candidate, ReplaySummary Summary);
