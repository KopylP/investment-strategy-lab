using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

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
