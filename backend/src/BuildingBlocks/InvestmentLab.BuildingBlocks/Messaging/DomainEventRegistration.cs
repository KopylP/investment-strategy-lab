using InvestmentLab.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Messaging;

public static class DomainEventRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddDomainEventsOutbox()
        {
            services.TryAddSingleton<DomainEventsToOutbox>();
            services.TryAddSingleton<DomainEventsInterceptor>();

            return services;
        }

        public IServiceCollection AddDomainEventMapper<TDomainEvent, TIntegrationEvent, TMapper>()
            where TDomainEvent : IDomainEvent
            where TIntegrationEvent : class
            where TMapper : class, IDomainEventMapper<TDomainEvent, TIntegrationEvent>, new()
        {
            var translation = new DomainEventTranslation<TDomainEvent, TIntegrationEvent>(new TMapper());

            services.AddSingleton<IDomainEventTranslation>(translation);

            return services;
        }
    }
}
