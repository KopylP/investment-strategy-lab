using InvestmentLab.BuildingBlocks.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InvestmentLab.BuildingBlocks.Messaging;

/// <summary>
/// Polls every module's outbox and delivers pending events in-process, oldest first.
/// An event nobody handles is marked processed; a failing one is retried up to <see cref="OutboxMessage.MaxAttempts"/>.
/// </summary>
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

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, clock);

        do
        {
            foreach (var context in contexts)
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
        }
        while (await WaitForNextTickAsync(timer, stoppingToken));
    }

    private async Task<int> DispatchBatchAsync(ModuleDbContextType context, CancellationToken stoppingToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var dbContext = (DbContext)scope.ServiceProvider.GetRequiredService(context.ContextType);

        var pending = await dbContext.Set<OutboxMessage>()
            .Where(message => message.ProcessedAt == null && message.Attempts < OutboxMessage.MaxAttempts)
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
            // Handlers get a fresh scope: their DbContexts are not the outbox's.
            await using var handlerScope = scopes.CreateAsyncScope();
            await delivery.DeliverAsync(handlerScope.ServiceProvider, message.Payload, stoppingToken);
            message.MarkProcessed(clock.GetUtcNow());
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "Delivery of {EventType} {MessageId} failed", message.Type, message.Id);
            message.RecordFailure(exception.Message);
        }
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
