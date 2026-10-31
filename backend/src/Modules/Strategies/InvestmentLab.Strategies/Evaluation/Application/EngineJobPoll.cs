using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application;

public sealed record EngineJobPoll<T>(StrategyJobState State, Result<T>? Result = null) where T : class;
