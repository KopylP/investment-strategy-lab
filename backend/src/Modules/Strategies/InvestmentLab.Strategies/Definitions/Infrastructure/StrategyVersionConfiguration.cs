using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using InvestmentLab.Strategies.Definitions.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyVersionConfiguration : IEntityTypeConfiguration<StrategyVersion>
{
    public void Configure(EntityTypeBuilder<StrategyVersion> version)
    {
        version.ToTable("strategy_versions");
        version.HasKey(v => v.Id);
        version.Property(v => v.Id).ValueGeneratedNever();
        version.Property(v => v.Parameters).HasColumnType("jsonb");
        version.Property(v => v.ExtraSeries).HasDefaultValueSql("'{}'::uuid[]");
        version.Property(v => v.SignalIds).HasDefaultValueSql("'{}'::uuid[]");
        version.Property(v => v.Reports).HasColumnType("jsonb").HasDefaultValueSql("'[]'::jsonb").HasConversion(
            reports => JsonSerializer.Serialize(reports, JsonSerializerOptions.Web),
            json => JsonSerializer.Deserialize<List<VersionReport>>(json, JsonSerializerOptions.Web) ?? new List<VersionReport>(),
            new ValueComparer<List<VersionReport>>(
                (a, b) => a!.Count == b!.Count && a.Select(JsonOf).SequenceEqual(b.Select(JsonOf)),
                reports => reports.Count,
                reports => reports.ToList()));
        version.HasIndex("StrategyId", nameof(StrategyVersion.Number)).IsUnique();
    }

    private static string JsonOf(VersionReport report) => JsonSerializer.Serialize(report, JsonSerializerOptions.Web);
}
