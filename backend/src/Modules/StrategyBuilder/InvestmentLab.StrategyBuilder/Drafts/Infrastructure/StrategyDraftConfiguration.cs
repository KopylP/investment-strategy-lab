using System.Text.Json;
using InvestmentLab.StrategyBuilder.Drafts.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal static class StrategyDraftConfiguration
{
    public static void Configure(EntityTypeBuilder<StrategyDraft> draft)
    {
        draft.ToTable("drafts");
        draft.HasKey(entry => entry.Id);
        draft.Property(entry => entry.Id).ValueGeneratedNever();
        draft.Property(entry => entry.OwnerId).HasConversion(id => id.Value, value => UserId.Of(value));
        draft.Property(entry => entry.Name).HasMaxLength(100);
        draft.Property(entry => entry.Description).HasMaxLength(2000);
        draft.Ignore(entry => entry.DomainEvents);

        AsJson(draft.Property(entry => entry.Setup));
        AsJson(draft.Property(entry => entry.Graph));
        AsJson(draft.Property(entry => entry.Layout));
        AsJson(draft.Property(entry => entry.Reports));
        AsJson(draft.Property(entry => entry.Origin));
        AsJson(draft.Property(entry => entry.Trials));
        AsJson(draft.Property(entry => entry.MissingSignalIds));
    }

    private static void AsJson<T>(PropertyBuilder<T> property) =>
        property.HasColumnType("jsonb").HasConversion(
            value => JsonSerializer.Serialize(value, DraftJson.Options),
            text => JsonSerializer.Deserialize<T>(text, DraftJson.Options)!,
            new ValueComparer<T>((left, right) => ReferenceEquals(left, right), value => value == null ? 0 : value.GetHashCode(), value => value));
}
