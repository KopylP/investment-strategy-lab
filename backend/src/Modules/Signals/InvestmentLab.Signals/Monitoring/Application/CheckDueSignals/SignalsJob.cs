using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.Signals.Monitoring.Application.CheckSignal;
using InvestmentLab.Signals.Monitoring.Application.EnsureBuiltInSignals;
using InvestmentLab.Signals.Monitoring.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.Signals.Monitoring.Application.CheckDueSignals;

public sealed class SignalsJob(
    ISignalRepository signals,
    EnsureBuiltInSignalsHandler builtInSignals,
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<SignalsJob> logger) : IScheduledJob
{
    public const string TimeZone = "Europe/Warsaw";

    public static IJobSchedule Schedule { get; } = new IntervalSchedule(TimeSpan.FromMinutes(1));

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await builtInSignals.Handle(cancellationToken);

        var dueSignalIds = await DueSignalIdsAsync(cancellationToken);
        foreach (var signalId in dueSignalIds)
            await CheckInOwnScopeAsync(signalId, cancellationToken);
    }

    private async Task<List<Guid>> DueSignalIdsAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var all = await signals.AllAsync(cancellationToken);

        return [.. all
            .Where(signal => signal.IsDue(now, ToLocal))
            .Select(signal => signal.Id)];
    }

    private async Task CheckInOwnScopeAsync(Guid signalId, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopes.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<CheckSignalHandler>();

            await handler.Handle(signalId, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Signal {SignalId} could not be checked", signalId);
        }
    }

    private static DateTime ToLocal(DateTimeOffset instant)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);

        return TimeZoneInfo.ConvertTime(instant, zone).DateTime;
    }
}
