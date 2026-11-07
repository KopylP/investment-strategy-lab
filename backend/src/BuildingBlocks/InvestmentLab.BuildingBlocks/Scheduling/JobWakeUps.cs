using System.Collections.Concurrent;
using System.Threading.Channels;

namespace InvestmentLab.BuildingBlocks.Scheduling;

internal sealed class JobWakeUps : IJobWakeUp
{
    private static readonly BoundedChannelOptions OnePendingWakeUp = new(1) { FullMode = BoundedChannelFullMode.DropWrite };

    private readonly ConcurrentDictionary<Type, Channel<bool>> _signals = new();

    public void Wake<TJob>() where TJob : IScheduledJob => SignalOf(typeof(TJob)).Writer.TryWrite(true);

    public async Task WaitAsync(Type jobType, CancellationToken cancellationToken) =>
        await SignalOf(jobType).Reader.ReadAsync(cancellationToken);

    private Channel<bool> SignalOf(Type jobType) =>
        _signals.GetOrAdd(jobType, _ => Channel.CreateBounded<bool>(OnePendingWakeUp));
}
