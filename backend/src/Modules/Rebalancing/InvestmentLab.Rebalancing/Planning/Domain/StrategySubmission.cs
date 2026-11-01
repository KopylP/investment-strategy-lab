namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed record StrategySubmission(StrategyJob? Job, string? ErrorCode, string? ErrorMessage, bool EngineUnavailable)
{
    public static StrategySubmission Accepted(StrategyJob job) => new(job, null, null, false);

    public static StrategySubmission Refused(string errorCode, string errorMessage, bool engineUnavailable) =>
        new(null, errorCode, errorMessage, engineUnavailable);
}
