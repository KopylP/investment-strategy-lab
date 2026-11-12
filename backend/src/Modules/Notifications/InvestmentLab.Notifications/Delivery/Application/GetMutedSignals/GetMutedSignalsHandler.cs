using InvestmentLab.SharedKernel;
using InvestmentLab.Notifications.Delivery.Domain;

namespace InvestmentLab.Notifications.Delivery.Application.GetMutedSignals;

public sealed class GetMutedSignalsHandler(INotificationPreferencesRepository preferences) : IHandler
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(UserId userId, CancellationToken cancellationToken)
    {
        var current = await preferences.FindAsync(userId, cancellationToken);
        IReadOnlyList<Guid> muted = current?.MutedSignalIds ?? [];

        return Result<IReadOnlyList<Guid>>.Success(muted);
    }
}
