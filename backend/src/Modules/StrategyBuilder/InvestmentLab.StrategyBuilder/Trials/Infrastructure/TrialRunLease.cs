namespace InvestmentLab.StrategyBuilder.Trials.Infrastructure;

internal sealed class TrialRunLease(SemaphoreSlim semaphore) : IAsyncDisposable
{
    private bool released;

    public ValueTask DisposeAsync()
    {
        if (!released)
        {
            released = true;
            semaphore.Release();
        }

        return ValueTask.CompletedTask;
    }
}
