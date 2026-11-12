using System.Reflection;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Reports.Domain;
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

    protected override Assembly MappingsAssembly => typeof(RebalancingAssemblyMarker).Assembly;
}
