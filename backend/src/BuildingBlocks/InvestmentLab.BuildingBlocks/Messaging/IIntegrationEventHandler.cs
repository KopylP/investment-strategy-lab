using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

/// <summary>
/// Reacts to an integration event of another (or the same) module. Delivery is at-least-once,
/// so a handler must be idempotent.
/// </summary>
public interface IIntegrationEventHandler<in TEvent>
    where TEvent : class
{
    Task Handle(TEvent integrationEvent, CancellationToken cancellationToken);
}

public static class IntegrationEventRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddIntegrationEventHandler<TEvent, THandler>()
            where TEvent : class
            where THandler : class, IIntegrationEventHandler<TEvent>
        {
            services.AddScoped<IIntegrationEventHandler<TEvent>, THandler>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IIntegrationEventDelivery, IntegrationEventDelivery<TEvent>>());
            return services;
        }
    }
}

/// <summary>Turns a stored payload back into its event and hands it to every handler.</summary>
internal interface IIntegrationEventDelivery
{
    string EventType { get; }

    Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken);
}

internal sealed class IntegrationEventDelivery<TEvent> : IIntegrationEventDelivery
    where TEvent : class
{
    public string EventType => IntegrationEvents.TypeName<TEvent>();

    public async Task DeliverAsync(IServiceProvider services, string payload, CancellationToken cancellationToken)
    {
        var integrationEvent = JsonSerializer.Deserialize<TEvent>(payload, IntegrationEvents.Json)
            ?? throw new InvalidOperationException($"Empty payload for {EventType}.");

        foreach (var handler in services.GetServices<IIntegrationEventHandler<TEvent>>())
            await handler.Handle(integrationEvent, cancellationToken);
    }
}

internal static class IntegrationEvents
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static string TypeName<TEvent>() => typeof(TEvent).FullName ?? typeof(TEvent).Name;
}
