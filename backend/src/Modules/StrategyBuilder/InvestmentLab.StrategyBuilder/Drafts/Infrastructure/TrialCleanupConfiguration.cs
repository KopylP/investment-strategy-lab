using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal sealed class TrialCleanupConfiguration : IEntityTypeConfiguration<TrialCleanup>
{
    public void Configure(EntityTypeBuilder<TrialCleanup> cleanup)
    {
        cleanup.ToTable("trial_cleanups");
        cleanup.HasKey(entry => entry.Id);
        cleanup.Property(entry => entry.Id).ValueGeneratedNever();
        cleanup.HasIndex(entry => entry.NextAttemptAt);
        cleanup.Ignore(entry => entry.DomainEvents);
    }
}
