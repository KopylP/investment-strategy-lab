using System.Reflection;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvestmentLab.BuildingBlocks.Modules;

public static class ModuleServiceScanner
{
    private const BindingFlags GenericHelper = BindingFlags.NonPublic | BindingFlags.Static;

    extension(IServiceCollection services)
    {
        public IServiceCollection AddServicesOf(Assembly assembly)
        {
            foreach (var type in ConcreteTypes(assembly))
            {
                AddHandler(services, type);
                AddIntegrationEventHandlers(services, type);
                AddDomainEventMappers(services, type);
                AddScheduledJob(services, type);
            }

            return services;
        }
    }

    private static IEnumerable<Type> ConcreteTypes(Assembly assembly) =>
        assembly.GetTypes().Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false });

    private static void AddHandler(IServiceCollection services, Type type)
    {
        if (type.IsAssignableTo(typeof(IHandler)))
            services.TryAddScoped(type);
    }

    private static void AddIntegrationEventHandlers(IServiceCollection services, Type type)
    {
        foreach (var eventType in GenericArgumentsOf(type, typeof(IIntegrationEventHandler<>)))
            InvokeGeneric(nameof(RegisterIntegrationEventHandler), [eventType[0], type], services);
    }

    private static void AddDomainEventMappers(IServiceCollection services, Type type)
    {
        foreach (var arguments in GenericArgumentsOf(type, typeof(IDomainEventMapper<,>)))
            InvokeGeneric(nameof(RegisterDomainEventMapper), [arguments[0], arguments[1], type], services);
    }

    private static void AddScheduledJob(IServiceCollection services, Type type)
    {
        if (type.IsAssignableTo(typeof(IScheduledJob)))
            InvokeGeneric(nameof(RegisterScheduledJob), [type], services);
    }

    private static IEnumerable<Type[]> GenericArgumentsOf(Type type, Type openInterface) =>
        type.GetInterfaces()
            .Where(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == openInterface)
            .Select(candidate => candidate.GetGenericArguments());

    private static void InvokeGeneric(string helper, Type[] typeArguments, IServiceCollection services)
    {
        var method = typeof(ModuleServiceScanner).GetMethod(helper, GenericHelper)!;
        method.MakeGenericMethod(typeArguments).Invoke(null, [services]);
    }

    private static void RegisterIntegrationEventHandler<TEvent, THandler>(IServiceCollection services)
        where TEvent : class
        where THandler : class, IIntegrationEventHandler<TEvent> =>
        services.AddIntegrationEventHandler<TEvent, THandler>();

    private static void RegisterDomainEventMapper<TDomainEvent, TIntegrationEvent, TMapper>(IServiceCollection services)
        where TDomainEvent : IDomainEvent
        where TIntegrationEvent : class
        where TMapper : class, IDomainEventMapper<TDomainEvent, TIntegrationEvent>, new() =>
        services.AddDomainEventMapper<TDomainEvent, TIntegrationEvent, TMapper>();

    private static void RegisterScheduledJob<TJob>(IServiceCollection services)
        where TJob : class, IScheduledJob =>
        services.AddScheduledJob<TJob>(TJob.Schedule, TJob.RunAtStartup);
}
