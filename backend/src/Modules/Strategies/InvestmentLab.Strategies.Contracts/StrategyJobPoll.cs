namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyJobPoll<T>(StrategyJobState State, StrategyEvaluationResult<T>? Result = null)
    where T : class;
