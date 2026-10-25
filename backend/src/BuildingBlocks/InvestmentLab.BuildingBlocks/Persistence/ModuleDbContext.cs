using System.Linq.Expressions;
using System.Text.Json;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>
/// Base of every module's DbContext: own schema, no cross-module mappings,
/// SharedKernel value objects stored as primitives, own outbox table.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    /// <summary>Named query filter: a signed-in user sees only own data; background work (no user) sees all.</summary>
    public const string OwnerFilter = "Owner";

    // A user id nobody has; stands in when there is no signed-in user so the filter stays one parameterised query.
    private static readonly UserId Nobody = UserId.Of(Guid.AllBitsSet);

    protected abstract string Schema { get; }

    /// <summary>Modules whose data belongs to users return the request's user here to enable <see cref="HasOwnerFilter{TEntity}"/>.</summary>
    protected virtual ICurrentUser? CurrentUser => null;

    protected bool HasCurrentUser => CurrentUser?.Id is not null;
    protected UserId CurrentOwner => CurrentUser?.Id ?? Nobody;

    public void HasOwnerFilter<TEntity>(EntityTypeBuilder<TEntity> entity, Expression<Func<TEntity, UserId>> owner)
        where TEntity : class
    {
        // EF Core re-binds this context constant to the context running each query.
        var context = Expression.Constant(this, typeof(ModuleDbContext));
        var visible = Expression.OrElse(
            Expression.Not(Expression.Property(context, nameof(HasCurrentUser))),
            Expression.Equal(owner.Body, Expression.Property(context, nameof(CurrentOwner))));
        entity.HasQueryFilter(OwnerFilter, Expression.Lambda<Func<TEntity, bool>>(visible, owner.Parameters[0]));
    }

    protected abstract void OnModuleModelCreating(ModelBuilder modelBuilder);

    /// <summary>Queues an integration event; it is stored by the next SaveChanges together with the change that raised it.</summary>
    public void Publish<TEvent>(TEvent integrationEvent, DateTimeOffset occurredAt)
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

        OnModuleModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Prices with sub-cent precision and fractional shares both fit.
        configurationBuilder.Properties<decimal>().HavePrecision(20, 8);

        configurationBuilder.Properties<Currency>().HaveConversion<SharedKernelConverters.CurrencyConverter>().HaveMaxLength(3);
        configurationBuilder.Properties<Isin>().HaveConversion<SharedKernelConverters.IsinConverter>().HaveMaxLength(12);
        configurationBuilder.Properties<UserId>().HaveConversion<SharedKernelConverters.UserIdConverter>();
        configurationBuilder.Properties<YearMonth>().HaveConversion<SharedKernelConverters.YearMonthConverter>().HaveMaxLength(7);
        configurationBuilder.Properties<Quantity>().HaveConversion<SharedKernelConverters.QuantityConverter>();
        configurationBuilder.Properties<Percentage>().HaveConversion<SharedKernelConverters.PercentageConverter>();
    }
}
