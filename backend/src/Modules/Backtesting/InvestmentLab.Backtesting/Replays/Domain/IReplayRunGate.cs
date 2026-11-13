namespace InvestmentLab.Backtesting.Replays.Domain;

public interface IReplayRunGate
{
    Task RunAsync(Func<Task> run, CancellationToken cancellationToken);
}
