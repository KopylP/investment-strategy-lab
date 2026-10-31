using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Planning.Infrastructure;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reports.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace InvestmentLab.Rebalancing.Shared.Infrastructure;

public sealed class RebalancingDbContext(DbContextOptions<RebalancingDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "rebalancing";

    public DbSet<RebalancingReport> Reports => Set<RebalancingReport>();
    public DbSet<PlanSettings> PlanSettings => Set<PlanSettings>();
    public DbSet<ReportRequest> ReportRequests => Set<ReportRequest>();
    public DbSet<SignalReportRule> SignalReportRules => Set<SignalReportRule>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RebalancingReportConfiguration(this));
        modelBuilder.ApplyConfiguration(new ReportActionConfiguration());
        modelBuilder.ApplyConfiguration(new PlanSettingsConfiguration(this));
        modelBuilder.ApplyConfiguration(new ReportRequestConfiguration(this));
        modelBuilder.ApplyConfiguration(new SignalReportRuleConfiguration(this));
    }
}
