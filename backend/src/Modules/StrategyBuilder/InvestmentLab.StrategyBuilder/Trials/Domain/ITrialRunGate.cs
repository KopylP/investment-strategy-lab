namespace InvestmentLab.StrategyBuilder.Trials.Domain;

public interface ITrialRunGate
{
    Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken);
}
