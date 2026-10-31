using System.Text.Json;
using InvestmentLab.Strategies.Catalog.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Strategies.Catalog.Infrastructure;

internal sealed class StrategyTypeConfiguration : IEntityTypeConfiguration<StrategyTypeDescriptor>
{
    public void Configure(EntityTypeBuilder<StrategyTypeDescriptor> type)
    {
        type.ToTable("strategy_types");
        type.HasKey(t => t.Code);
        type.Property(t => t.Code).HasMaxLength(100);
        type.Property(t => t.Name).HasMaxLength(200);
        type.Property(t => t.Engine).HasMaxLength(50);
        type.Property(t => t.EngineVersion).HasMaxLength(100);
        type.Property(t => t.ParametersSchema).HasColumnType("jsonb");
        type.Property(t => t.DefaultParameters).HasColumnType("jsonb");
        type.Property(t => t.Reports).HasColumnType("jsonb").HasConversion(
            reports => JsonSerializer.Serialize(reports, JsonSerializerOptions.Web),
            json => JsonSerializer.Deserialize<List<ReportDefinition>>(json, JsonSerializerOptions.Web) ?? new List<ReportDefinition>(),
            new ValueComparer<List<ReportDefinition>>(
                (a, b) => a!.SequenceEqual(b!),
                reports => reports.Aggregate(0, (hash, report) => HashCode.Combine(hash, report)),
                reports => reports.ToList()));
    }
}
