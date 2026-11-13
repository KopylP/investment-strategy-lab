using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.BuildingBlocks.Messaging;

internal sealed class OutboxDispatcher(
    IEnumerable<ModuleDbContextType> contexts,
    IEnumerable<IIntegrationEventDelivery> deliveries,
    IServiceScopeFactory scopes,
    TimeProvider clock,
    ILogger<OutboxDispatcher> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<string, IIntegrationEventDelivery> _deliveries =
        deliveries.ToDictionary(delivery => delivery.EventType);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var loops = contexts.Select(context => DispatchContinuouslyAsync(context, stoppingToken));

        return Task.WhenAll(loops);
    }

    private async Task DispatchContinuouslyAsync(ModuleDbContextType context, CancellationToken stoppingToken)
    {
        await Task.Yield();
        using var timer = new PeriodicTimer(PollInterval, clock);

        do
        {
            await DispatchPendingAsync(context, stoppingToken);
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task DispatchPendingAsync(ModuleDbContextType context, CancellationToken stoppingToken)
    {
        try
        {
            while (await DispatchBatchAsync(context, stoppingToken) == BatchSize) { }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Outbox of {Context} could not be dispatched", context.ContextType.Name);
        }
    }

    internal async Task<int> DispatchBatchAsync(ModuleDbContextType context, CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(context.ContextType);

        var pending = await dbContext.Set<OutboxMessage>()
            .Where(OutboxMessage.DueAt(clock.GetUtcNow()))
            .OrderBy(message => message.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in pending)
        {
            await DeliverAsync(message, stoppingToken);
            await dbContext.SaveChangesAsync(stoppingToken);
        }

        return pending.Count;
    }

    private async Task DeliverAsync(OutboxMessage message, CancellationToken stoppingToken)
    {
        if (!_deliveries.TryGetValue(message.Type, out var delivery))
        {
            message.MarkProcessed(clock.GetUtcNow());

            return;
        }

        try
        {
            await using var handlerScope = scopes.CreateAsyncScope();
            await delivery.DeliverAsync(handlerScope.ServiceProvider, message.Payload, stoppingToken);
            message.MarkProcessed(clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            message.RecordFailure(exception.Message, clock.GetUtcNow());
            ReportFailure(message, exception);
        }
    }

    private void ReportFailure(OutboxMessage message, Exception exception)
    {
        if (message.IsAbandoned)
        {
            logger.LogError(
                exception,
                "Delivery of {EventType} {MessageId} failed {Attempts} times and is given up",
                message.Type,
                message.Id,
                message.Attempts);

            return;
        }

        logger.LogWarning(
            exception,
            "Delivery of {EventType} {MessageId} failed, attempt {Attempts} of {MaxAttempts}, next at {NextAttemptAt:O}",
            message.Type,
            message.Id,
            message.Attempts,
            OutboxMessage.MaxAttempts,
            message.NextAttemptAt);
    }

    private static async Task<bool> WaitForNextTickAsync(PeriodicTimer timer, CancellationToken stoppingToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
