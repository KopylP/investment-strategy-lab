using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

internal sealed class NotificationPreferencesConfiguration : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> preferences)
    {
        preferences.ToTable("preferences");
        preferences.HasKey(p => p.Id);
        preferences.Property(p => p.Id).HasColumnName("user_id").ValueGeneratedNever();
        preferences.Property(p => p.MutedSignalIds).HasDefaultValueSql("'{}'::uuid[]");
        preferences.Ignore(p => p.DomainEvents);
    }
}
