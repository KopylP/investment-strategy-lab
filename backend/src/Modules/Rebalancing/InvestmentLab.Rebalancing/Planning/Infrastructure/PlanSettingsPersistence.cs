using InvestmentLab.Rebalancing.Planning.Application;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Rebalancing.Planning.Infrastructure;

internal sealed class PlanSettingsConfiguration(RebalancingDbContext context) : IEntityTypeConfiguration<PlanSettings>
{
    public void Configure(EntityTypeBuilder<PlanSettings> settings)
    {
        settings.ToTable("plan_settings");
        settings.HasKey(s => s.Id);
        settings.Property(s => s.Id).HasColumnName("account_id").ValueGeneratedNever();
        settings.Ignore(s => s.AccountId);
        settings.Ignore(s => s.Policy);
        settings.Property(s => s.ReportSchedules).HasJsonConversion();
        settings.Ignore(s => s.DomainEvents);
        context.HasOwnerFilter(settings, s => s.OwnerId);
    }
}

internal sealed class PlanSettingsRepository(RebalancingDbContext db) : IPlanSettingsRepository
{
    public Task<PlanSettings?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        db.PlanSettings.SingleOrDefaultAsync(settings => settings.Id == accountId, cancellationToken);

    public void Add(PlanSettings settings) => db.PlanSettings.Add(settings);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
