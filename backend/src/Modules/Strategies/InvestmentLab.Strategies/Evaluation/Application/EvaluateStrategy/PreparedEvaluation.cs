using InvestmentLab.Strategies.Definitions.Domain;

namespace InvestmentLab.Strategies.Evaluation.Application.EvaluateStrategy;

public sealed record PreparedEvaluation(Strategy Strategy, StrategyVersion Version, StrategyEvaluationContext Context);
