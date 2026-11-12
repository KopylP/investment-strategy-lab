using System.Reflection;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.BuildingBlocks.Tests.Messaging;

public sealed class DomainEventsToOutboxTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 22, 0, 0, TimeSpan.Zero);

    private sealed record ThingHappened(string What, DateTimeOffset OccurredAt) : IDomainEvent;

    private sealed record ThingStayedInside(DateTimeOffset OccurredAt) : IDomainEvent;

    public sealed record ThingHappenedV1(string What);

    private sealed class ThingMapper : IDomainEventMapper<ThingHappened, ThingHappenedV1>
    {
        public ThingHappenedV1 Map(ThingHappened domainEvent) => new(domainEvent.What);
    }

    private sealed class Thing(Guid id) : AggregateRoot<Guid>(id)
    {
        public void Happen(IDomainEvent domainEvent) => Raise(domainEvent);
    }

    private sealed class ThingsDbContext(DbContextOptions<ThingsDbContext> options) : ModuleDbContext(options)
    {
        protected override string Schema => "things";

        protected override Assembly MappingsAssembly => typeof(DomainEventsToOutboxTests).Assembly;

        protected override bool IsMappedHere(Type configuration) => configuration.DeclaringType == typeof(DomainEventsToOutboxTests);
    }

    private sealed class ThingConfiguration : IEntityTypeConfiguration<Thing>
    {
        public void Configure(EntityTypeBuilder<Thing> builder)
        {
            builder.HasKey(each => each.Id);
            builder.Ignore(each => each.DomainEvents);
        }
    }

    private static ThingsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ThingsDbContext>()
            .UseNpgsql("Host=unused")
            .UseSnakeCaseNamingConvention()
            .Options);

    private static DomainEventsToOutbox Outbox() =>
        new([new DomainEventTranslation<ThingHappened, ThingHappenedV1>(new ThingMapper())]);

    private static List<OutboxMessage> OutboxMessagesIn(ThingsDbContext context) =>
        [.. context.ChangeTracker.Entries<OutboxMessage>().Select(entry => entry.Entity)];

    [Fact]
    public void A_raised_event_becomes_an_outbox_message_of_its_integration_event()
    {
        using var context = CreateContext();
        var thing = new Thing(Guid.NewGuid());
        thing.Happen(new ThingHappened("fell", Now));
        context.Add(thing);

        Outbox().Move(context);

        var message = OutboxMessagesIn(context).ShouldHaveSingleItem();
        message.Type.ShouldEndWith(nameof(ThingHappenedV1));
        message.Payload.ShouldContain("fell");
        message.OccurredAt.ShouldBe(Now);
    }

    [Fact]
    public void Events_are_taken_once_so_a_second_save_publishes_nothing_more()
    {
        using var context = CreateContext();
        var thing = new Thing(Guid.NewGuid());
        thing.Happen(new ThingHappened("fell", Now));
        context.Add(thing);

        Outbox().Move(context);
        Outbox().Move(context);

        thing.DomainEvents.ShouldBeEmpty();
        OutboxMessagesIn(context).ShouldHaveSingleItem();
    }

    [Fact]
    public void An_aggregate_being_deleted_still_publishes_its_events()
    {
        using var context = CreateContext();
        var thing = new Thing(Guid.NewGuid());
        context.Attach(thing);
        thing.Happen(new ThingHappened("deleted", Now));
        context.Remove(thing);

        Outbox().Move(context);

        OutboxMessagesIn(context).ShouldHaveSingleItem();
    }

    [Fact]
    public void An_event_without_a_mapper_stays_inside_the_module()
    {
        using var context = CreateContext();
        var thing = new Thing(Guid.NewGuid());
        thing.Happen(new ThingStayedInside(Now));
        context.Add(thing);

        Outbox().Move(context);

        OutboxMessagesIn(context).ShouldBeEmpty();
        thing.DomainEvents.ShouldBeEmpty();
    }
}
