using System.Text.Json;
using InvestmentLab.BuildingBlocks.Messaging;
using InvestmentLab.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.BuildingBlocks.Persistence;

/// <summary>
/// Base of every module's DbContext: own schema, no cross-module mappings,
/// SharedKernel value objects stored as primitives, own outbox table.
/// </summary>
public abstract class ModuleDbContext(DbContextOptions options) : DbContext(options)
{
    protected abstract string Schema { get; }

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
