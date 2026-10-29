using InvestmentLab.Notifications.Delivery.Domain;
using InvestmentLab.Notifications.Shared.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InvestmentLab.Notifications.Delivery.Infrastructure;

internal sealed class NotificationPreferencesConfiguration(NotificationsDbContext context) : IEntityTypeConfiguration<NotificationPreferences>
{
    public void Configure(EntityTypeBuilder<NotificationPreferences> preferences)
    {
        preferences.ToTable("preferences");
        preferences.HasKey(p => p.Id);
        preferences.Property(p => p.Id).HasColumnName("user_id").ValueGeneratedNever();
        preferences.Ignore(p => p.DomainEvents);
        context.HasOwnerFilter(preferences, p => p.OwnerId);
    }
}

internal sealed class NotificationPreferencesRepository(NotificationsDbContext db) : INotificationPreferencesRepository
{
    public Task<NotificationPreferences?> FindAsync(UserId ownerId, CancellationToken cancellationToken) =>
        db.Preferences.SingleOrDefaultAsync(preferences => preferences.Id == ownerId.Value, cancellationToken);

    public void Add(NotificationPreferences preferences) => db.Preferences.Add(preferences);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);
}
