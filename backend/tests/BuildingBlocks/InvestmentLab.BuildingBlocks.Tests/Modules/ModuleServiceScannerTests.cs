using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Modules;
using InvestmentLab.BuildingBlocks.Scheduling;
using InvestmentLab.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace InvestmentLab.BuildingBlocks.Tests.Modules;

public sealed class ModuleServiceScannerTests
{
    public sealed record SomethingHappened(string What) : IDomainEvent
    {
        public DateTimeOffset OccurredAt => DateTimeOffset.UnixEpoch;
    }

    public sealed record SomethingHappenedV1(string What);

    public sealed class SampleHandler : IHandler;

    public sealed class SampleEventHandler : IIntegrationEventHandler<SomethingHappenedV1>
    {
        public Task Handle(SomethingHappenedV1 integrationEvent, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class SampleMapper : IDomainEventMapper<SomethingHappened, SomethingHappenedV1>
    {
        public SomethingHappenedV1 Map(SomethingHappened domainEvent) => new(domainEvent.What);
    }

    public sealed class SampleJob : IScheduledJob
    {
        public static IJobSchedule Schedule { get; } = new IntervalSchedule(TimeSpan.FromMinutes(7));

        public static bool RunAtStartup => true;

        public Task RunAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static ServiceCollection Scanned()
    {
        var services = new ServiceCollection();
        services.AddServicesOf(typeof(ModuleServiceScannerTests).Assembly);

        return services;
    }

    [Fact]
    public void A_handler_is_registered_for_a_scope_as_itself()
    {
        var descriptor = Scanned().Single(service => service.ServiceType == typeof(SampleHandler));

        descriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void An_integration_event_handler_is_registered_with_its_delivery()
    {
        var services = Scanned();

        services.ShouldContain(service =>
            service.ServiceType == typeof(IIntegrationEventHandler<SomethingHappenedV1>) && service.ImplementationType == typeof(SampleEventHandler));
        services.ShouldContain(service => service.ImplementationType == typeof(IntegrationEventDelivery<SomethingHappenedV1>));
    }

    [Fact]
    public void A_domain_event_mapper_is_registered_as_a_translation()
    {
        var services = Scanned();

        services.ShouldContain(service => service.ImplementationInstance is DomainEventTranslation<SomethingHappened, SomethingHappenedV1>);
    }

    [Fact]
    public void A_job_is_registered_with_the_schedule_and_startup_choice_it_declares()
    {
        var registration = Scanned()
            .Select(service => service.ImplementationInstance)
            .OfType<ScheduledJobRegistration>()
            .Single(job => job.JobType == typeof(SampleJob));

        registration.Schedule.ShouldBe(SampleJob.Schedule);
        registration.RunAtStartup.ShouldBeTrue();
    }

    [Fact]
    public void Scanning_twice_does_not_register_a_handler_twice()
    {
        var services = new ServiceCollection();
        services.AddServicesOf(typeof(ModuleServiceScannerTests).Assembly);
        services.AddServicesOf(typeof(ModuleServiceScannerTests).Assembly);

        services.Count(service => service.ServiceType == typeof(SampleHandler)).ShouldBe(1);
    }
}
