using InvestmentLab.SharedKernel;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application.HearSignal;

public sealed record HearSignalCommand(bool Notify);

public sealed class HearSignalHandler(INotificationPreferencesRepository preferences, TimeProvider clock) : IHandler
{
    public async Task<Result> Handle(UserId userId, Guid signalId, HearSignalCommand command, CancellationToken cancellationToken)
    {
        var current = await FindOrCreateAsync(userId, cancellationToken);

        current.HearSignal(signalId, command.Notify, clock.GetUtcNow());
        await preferences.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task<NotificationPreferences> FindOrCreateAsync(UserId userId, CancellationToken cancellationToken)
    {
        var existing = await preferences.FindAsync(userId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = NotificationPreferences.For(userId);
        preferences.Add(created);

        return created;
    }
}
