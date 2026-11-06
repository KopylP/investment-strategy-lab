using InvestmentLab.StrategyBuilder.Trials.Domain;

namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialRunGate : ITrialRunGate, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<IAsyncDisposable> EnterAsync(CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        return new TrialRunLease(_semaphore);
    }

    public void Dispose() => _semaphore.Dispose();
}
