using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal static class BacktestStatuses
{
    public const string NotSupported = "NotSupported";

    public static TrialStatus TrialStatusOf(string? status) => status switch
    {
        null or "Queued" => TrialStatus.Waiting,
        "Running" => TrialStatus.Running,
        "Succeeded" => TrialStatus.Succeeded,
        "Failed" or NotSupported => TrialStatus.Failed,
        "Cancelled" => TrialStatus.Cancelled,
        _ => throw new InvalidOperationException($"Unknown backtest status '{status}'."),
    };
}
