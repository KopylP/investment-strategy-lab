namespace InvestmentLab.Backtesting.Experiments.Domain;

public enum ExperimentStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Cancelled,
}
