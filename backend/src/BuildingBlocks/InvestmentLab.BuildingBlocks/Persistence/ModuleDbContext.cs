using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.BuildingBlocks.Persistence;

public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    public const string OwnerFilter = "Owner";

    private static readonly UserId Nobody = UserId.Of(Guid.AllBitsSet);

    private readonly List<IDomainEvent> _eventsWithoutAggregate = [];

    protected abstract string Schema { get; }

    protected virtual ICurrentUser? CurrentUser => null;

    protected bool HasCurrentUser => CurrentUser?.Id is not null;
    protected UserId CurrentOwner => CurrentUser?.Id ?? Nobody;

    protected abstract Assembly MappingsAssembly { get; }

    private LambdaExpression OwnerVisibility(Type entityType)
    {
        var entity = Expression.Parameter(entityType, "entity");
        var owner = Expression.Property(entity, nameof(IUserOwned.OwnerId));
        var context = Expression.Constant(this, typeof(ModuleDbContext));
        var visible = Expression.OrElse(
            Expression.Not(Expression.Property(context, nameof(HasCurrentUser))),
            Expression.Equal(owner, Expression.Property(context, nameof(CurrentOwner))));

        return Expression.Lambda(visible, entity);
    }

    private void FilterByOwner(ModelBuilder modelBuilder)
    {
        var owned = modelBuilder.Model.GetEntityTypes().Where(entity => entity.ClrType.IsAssignableTo(typeof(IUserOwned)));

        foreach (var entityType in owned)
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(OwnerFilter, OwnerVisibility(entityType.ClrType));
    }

    public void Record(IDomainEvent domainEvent) => _eventsWithoutAggregate.Add(domainEvent);

    internal IReadOnlyList<IDomainEvent> TakeEventsWithoutAggregate()
    {
        var taken = _eventsWithoutAggregate.ToList();
        _eventsWithoutAggregate.Clear();

        return taken;
    }

    internal void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt)
        where TEvent : class =>
        Set<OutboxMessage>().Add(OutboxMessage.For(
            IntegrationEvents.TypeName<TEvent>(), JsonSerializer.Serialize(integrationEvent, IntegrationEvents.Json), occurredAt));

    protected sealed override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<OutboxMessage>(outbox =>
        {
            outbox.ToTable("outbox_messages");
            outbox.HasKey(message => message.Id);
            outbox.Property(message => message.Type).HasMaxLength(500);
            outbox.Property(message => message.Payload).HasColumnType("jsonb");
            outbox.HasIndex(message => message.OccurredAt).HasFilter("processed_at IS NULL");
        });

        modelBuilder.ApplyConfigurationsFromAssembly(MappingsAssembly);
        FilterByOwner(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(20, 8);

        configurationBuilder.Properties<Currency>().HaveConversion<SharedKernelConverters.CurrencyConverter>().HaveMaxLength(3);
        configurationBuilder.Properties<Language>().HaveConversion<SharedKernelConverters.LanguageConverter>().HaveMaxLength(8);
        configurationBuilder.Properties<Isin>().HaveConversion<SharedKernelConverters.IsinConverter>().HaveMaxLength(12);
        configurationBuilder.Properties<UserId>().HaveConversion<SharedKernelConverters.UserIdConverter>();
        configurationBuilder.Properties<YearMonth>().HaveConversion<SharedKernelConverters.YearMonthConverter>().HaveMaxLength(7);
        configurationBuilder.Properties<Quantity>().HaveConversion<SharedKernelConverters.QuantityConverter>();
        configurationBuilder.Properties<Percentage>().HaveConversion<SharedKernelConverters.PercentageConverter>();
    }
}
