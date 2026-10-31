namespace InvestmentLab.Strategies.Contracts;

public sealed record StrategyEvaluationResult<T>(T? Value, string? ErrorCode = null, string? ErrorMessage = null)
    where T : class
{
    public bool IsSuccess => ErrorCode is null;
}
