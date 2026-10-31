using InvestmentLab.Signals.Monitoring.Domain;
using InvestmentLab.Signals.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal sealed class SignalOccurrenceConfiguration(SignalsDbContext context) : IEntityTypeConfiguration<SignalOccurrence>
{
    public void Configure(EntityTypeBuilder<SignalOccurrence> occurrence)
    {
        occurrence.ToTable("signal_occurrences");
        occurrence.HasKey(o => o.Id);
        occurrence.Property(o => o.Id).ValueGeneratedNever();

        occurrence.Property(o => o.SubjectKey).HasMaxLength(100);
        occurrence.Property(o => o.SubjectLabel).HasMaxLength(200);
        occurrence.Property(o => o.ClearReason).HasConversion<string>().HasMaxLength(20);

        occurrence.Ignore(o => o.IsOpen);
        occurrence.Ignore(o => o.DomainEvents);

        occurrence.HasOne<Signal>().WithMany().HasForeignKey(o => o.SignalId).OnDelete(DeleteBehavior.Cascade);
        occurrence.HasIndex(o => new { o.SignalId, o.SubjectKey }).IsUnique().HasFilter("cleared_at IS NULL");
        occurrence.HasIndex(o => o.TriggeredAt);

        context.HasOwnerFilter(occurrence, o => o.OwnerId);
    }
}
