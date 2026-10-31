using InvestmentLab.Strategies.Contracts;

namespace InvestmentLab.Strategies.Evaluation.Application.Shared;

public sealed record EngineJobPoll<T>(StrategyJobState State, Result<T>? Result = null) where T : class;
