using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Performance.Shadow.Domain;
using InvestmentLab.Performance.Valuation.Domain;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Performance.Shared.Infrastructure;

public sealed class PerformanceDbContext(DbContextOptions<PerformanceDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "performance";

    public DbSet<ValuationSnapshot> Snapshots => Set<ValuationSnapshot>();
    public DbSet<RecalculationMarker> Markers => Set<RecalculationMarker>();
    public DbSet<PlannedReport> PlannedReports => Set<PlannedReport>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ValuationSnapshot>(snapshot =>
        {
            snapshot.ToTable("valuation_snapshots");
            snapshot.HasKey(s => s.Id);
            snapshot.Property(s => s.Scenario).HasConversion<string>().HasMaxLength(20);
            snapshot.HasIndex(s => new { s.AccountId, s.Scenario, s.Date });
            HasOwnerFilter(snapshot, s => s.OwnerId);
        });

        modelBuilder.Entity<RecalculationMarker>(marker =>
        {
            marker.ToTable("recalculation_markers");
            marker.HasKey(m => m.AccountId);
            marker.Ignore(m => m.IsDirty);
            HasOwnerFilter(marker, m => m.OwnerId);
        });

        modelBuilder.Entity<PlannedReport>(report =>
        {
            report.ToTable("planned_reports");
            report.HasKey(r => r.ReportId);
            report.Property(r => r.Kind).HasMaxLength(20);
            report.Property(r => r.Period).HasMaxLength(7);
            report.Property(r => r.Actions).HasJsonConversion();
            report.HasIndex(r => r.AccountId);
            report.Ignore(r => r.CountsForPlan);
            HasOwnerFilter(report, r => r.OwnerId);
        });
    }
}
