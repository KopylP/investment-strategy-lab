using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Domain;

public sealed record ResearchStudy(
    string Kind,
    ResearchFold Window,
    decimal? ParameterValue,
    ReplaySummary? Summary,
    double? TrainingSharpe = null,
    string? FailureReason = null);
