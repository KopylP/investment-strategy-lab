using InvestmentLab.Signals.Monitoring.Domain;
using InvestmentLab.Signals.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class SignalConfiguration(SignalsDbContext context) : IEntityTypeConfiguration<Signal>
{
    public void Configure(EntityTypeBuilder<Signal> signal)
    {
        signal.ToTable("signals");
        signal.HasKey(s => s.Id);
        signal.Property(s => s.Id).ValueGeneratedNever();

        signal.Property(s => s.Name).HasMaxLength(Signal.MaxNameLength);
        signal.Property(s => s.BuiltInKey).HasMaxLength(100);
        signal.Property(
            s => s.Target).HasColumnType("jsonb").HasConversion(JsonbValue.Converter<SignalTarget>(),
            JsonbValue.Comparer<SignalTarget>());
        signal.Property(
            s => s.Condition).HasColumnType("jsonb").HasConversion(JsonbValue.Converter<SignalCondition>(),
            JsonbValue.Comparer<SignalCondition>());
        signal.Property(s => s.Schedule).HasColumnType("jsonb").HasConversion(JsonbValue.Converter<Cadence>(), JsonbValue.Comparer<Cadence>());

        signal.Ignore(s => s.IsBuiltIn);
        signal.Ignore(s => s.DomainEvents);

        signal.HasIndex(s => s.BuiltInKey).IsUnique().HasFilter("built_in_key IS NOT NULL");

        context.HasOwnerFilter(signal, s => s.OwnerId);
    }
}
