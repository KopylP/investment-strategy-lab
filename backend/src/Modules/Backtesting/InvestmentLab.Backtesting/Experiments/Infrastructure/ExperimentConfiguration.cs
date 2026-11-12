using System.Text.Json;
using InvestmentLab.Backtesting.Experiments.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Backtesting.Experiments.Infrastructure;

internal sealed class ExperimentConfiguration : IEntityTypeConfiguration<ResearchExperiment>
{
    private const int StatusLength = 20;
    private const int FailureReasonLength = 2000;
    private const string ConcurrencyVersion = "Version";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<ResearchExperiment> entity)
    {
        entity.ToTable("experiments");
        entity.HasKey(item => item.Id);
        entity.Property(item => item.Id).ValueGeneratedNever();
        entity.Ignore(item => item.DomainEvents);
        entity.Property(item => item.Status).HasConversion<string>().HasMaxLength(StatusLength);
        entity.Property(item => item.FailureReason).HasMaxLength(FailureReasonLength);
        entity.Property<uint>(ConcurrencyVersion).IsRowVersion();
        entity.HasIndex(item => new { item.StrategyId, item.StrategyVersion });

        entity.Property(item => item.Conditions)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, Json),
                text => JsonSerializer.Deserialize<ExperimentConditions>(text, Json)!,
                ReferenceComparer<ExperimentConditions>());

        entity.Property(item => item.Result)
            .HasColumnType("jsonb")
            .HasConversion(
                value => value == null ? null : JsonSerializer.Serialize(value, Json),
                text => text == null ? null : JsonSerializer.Deserialize<ResearchReport>(text, Json),
                ReferenceComparer<ResearchReport?>());
    }

    private static ValueComparer<T> ReferenceComparer<T>() => new(
        (left, right) => ReferenceEquals(left, right),
        value => value == null ? 0 : value.GetHashCode());
}
