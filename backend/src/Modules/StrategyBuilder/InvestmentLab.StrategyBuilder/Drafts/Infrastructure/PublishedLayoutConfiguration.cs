using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal static class PublishedLayoutConfiguration
{
    public static void Configure(EntityTypeBuilder<PublishedLayout> layout)
    {
        layout.ToTable("published_layouts");
        layout.HasKey(entry => entry.Id);
        layout.Property(entry => entry.Id).ValueGeneratedNever();
        layout.HasIndex(entry => new { entry.StrategyId, entry.Version }).IsUnique();
        layout.Ignore(entry => entry.DomainEvents);
        AsJson(layout.Property(entry => entry.Graph));
        AsJson(layout.Property(entry => entry.Layout));
    }

    private static void AsJson<T>(PropertyBuilder<T> property) =>
        property.HasColumnType("jsonb").HasConversion(
            value => JsonSerializer.Serialize(value, DraftJson.Options),
            text => JsonSerializer.Deserialize<T>(text, DraftJson.Options)!,
            new ValueComparer<T>((left, right) => ReferenceEquals(left, right), value => value == null ? 0 : value.GetHashCode(), value => value));
}
