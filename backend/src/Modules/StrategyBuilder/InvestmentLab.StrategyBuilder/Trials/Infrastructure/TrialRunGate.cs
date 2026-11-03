using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialRunGate : ITrialRunGate, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    public async Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        return new TrialRunLease(semaphore);
    }

    public void Dispose() => semaphore.Dispose();
}
