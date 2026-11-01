namespace InvestmentLab.Backtesting.Replays.Domain;

public enum BacktestStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    NotSupported,
}
