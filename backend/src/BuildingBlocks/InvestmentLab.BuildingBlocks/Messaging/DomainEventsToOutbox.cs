using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.SharedKernel;

namespace InvestmentLab.BuildingBlocks.Messaging;

public sealed class DomainEventsToOutbox(IEnumerable<IDomainEventTranslation> translations)
{
    private readonly ILookup<Type, IDomainEventTranslation> _translationsByEvent =
        translations.ToLookup(translation => translation.DomainEventType);

    public void Move(ModuleDbContext context)
    {
        foreach (var aggregate in AggregatesWithEvents(context))
        {
            Publish(context, aggregate.DomainEvents);
            aggregate.ClearDomainEvents();
        }

        Publish(context, context.TakeEventsWithoutAggregate());
    }

    private static List<IHasDomainEvents> AggregatesWithEvents(ModuleDbContext context)
    {
        var tracked = context.ChangeTracker.Entries<IHasDomainEvents>();

        return [.. tracked
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)];
    }

    private void Publish(ModuleDbContext context, IReadOnlyList<IDomainEvent> domainEvents)
    {
        foreach (var domainEvent in domainEvents)
        {
            foreach (var translation in _translationsByEvent[domainEvent.GetType()])
                translation.PublishTo(context, domainEvent);
        }
    }
}
