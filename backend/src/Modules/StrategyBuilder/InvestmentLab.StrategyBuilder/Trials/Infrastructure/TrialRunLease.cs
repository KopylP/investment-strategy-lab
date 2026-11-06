namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialRunLease(SemaphoreSlim semaphore) : IAsyncDisposable
{
    private bool _released;

    public ValueTask DisposeAsync()
    {
        if (!_released)
        {
            _released = true;
            semaphore.Release();
        }

        return ValueTask.CompletedTask;
    }
}
