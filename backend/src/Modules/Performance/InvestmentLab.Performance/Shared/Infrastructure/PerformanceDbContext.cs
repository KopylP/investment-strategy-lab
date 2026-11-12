using System.Reflection;
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

    protected override Assembly MappingsAssembly => typeof(PerformanceAssemblyMarker).Assembly;
}
