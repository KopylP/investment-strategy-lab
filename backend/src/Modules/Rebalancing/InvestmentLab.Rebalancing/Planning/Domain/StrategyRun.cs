namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategyRun(
    Guid SubPortfolioId, StrategyJob? Job = null, string? Answer = null, string? ErrorCode = null, string? ErrorMessage = null, int Submissions = 0)
{
    public bool IsDone => Answer is not null || ErrorCode is not null;
}
