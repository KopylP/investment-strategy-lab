using System.Text.Json;
using InvestmentLab.BuildingBlocks.Persistence;
using InvestmentLab.BuildingBlocks.Security;
using InvestmentLab.Rebalancing.Planning.Domain;
using InvestmentLab.Rebalancing.Planning.Infrastructure;
using InvestmentLab.Rebalancing.Reports.Domain;
using InvestmentLab.Rebalancing.Reports.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Rebalancing.Shared.Infrastructure;

public sealed class RebalancingDbContext(DbContextOptions<RebalancingDbContext> options, ICurrentUser currentUser) : ModuleDbContext(options)
{
    public const string SchemaName = "rebalancing";

    public DbSet<RebalancingReport> Reports => Set<RebalancingReport>();
    public DbSet<PlanSettings> PlanSettings => Set<PlanSettings>();

    protected override string Schema => SchemaName;

    protected override ICurrentUser CurrentUser => currentUser;

    protected override void OnModuleModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RebalancingReportConfiguration(this));
        modelBuilder.ApplyConfiguration(new ReportActionConfiguration());
        modelBuilder.ApplyConfiguration(new PlanSettingsConfiguration(this));
    }
}

/// <summary>Snapshots without rules of their own (sections, fills, allocations) are stored as jsonb.</summary>
internal static class JsonColumn
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> property)
    {
        var converter = new ValueConverter<T, string>(
            value => JsonSerializer.Serialize(value, Options),
            json => JsonSerializer.Deserialize<T>(json, Options)!);
        var comparer = new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
            value => JsonSerializer.Serialize(value, Options).GetHashCode(),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!);
        return property.HasConversion(converter, comparer).HasColumnType("jsonb");
    }
}

internal sealed class RebalancingDesignTimeDbContextFactory : ModuleDesignTimeDbContextFactory<RebalancingDbContext>
{
    protected override string Schema => RebalancingDbContext.SchemaName;

    protected override RebalancingDbContext Create(DbContextOptions<RebalancingDbContext> options) => new(options, new NoUser());

    private sealed class NoUser : ICurrentUser
    {
        public UserId? Id => null;
    }
}
