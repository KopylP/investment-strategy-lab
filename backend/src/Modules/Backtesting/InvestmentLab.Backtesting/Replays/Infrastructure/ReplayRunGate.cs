using InvestmentLab.Backtesting.Replays.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InvestmentLab.Backtesting.Replays.Infrastructure;

internal sealed class ReplayRunGate(IOptions<ReplayRunOptions> options, ILogger<ReplayRunGate> logger) : IReplayRunGate, IDisposable
{
    private const long BytesPerMegabyte = 1024 * 1024;

    private readonly SemaphoreSlim _slots = new(options.Value.MaxConcurrentRuns);

    public async Task RunAsync(Func<Task> run, CancellationToken cancellationToken)
    {
        await _slots.WaitAsync(cancellationToken);

        try
        {
            WarnWhenMemoryIsHigh();
            await run();
        }
        finally
        {
            _slots.Release();
        }
    }

    public void Dispose() => _slots.Dispose();

    private void WarnWhenMemoryIsHigh()
    {
        var usedMegabytes = Environment.WorkingSet / BytesPerMegabyte;
        var thresholdMegabytes = options.Value.MemoryWarningMegabytes;
        if (usedMegabytes <= thresholdMegabytes)
            return;

        logger.LogWarning(
            "A replay starts while the process uses {UsedMegabytes} MB, above the warning threshold of {ThresholdMegabytes} MB",
            usedMegabytes,
            thresholdMegabytes);
    }
}
