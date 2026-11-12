using InvestmentLab.SharedKernel;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.DeleteSignal;

public sealed class DeleteSignalHandler(ISignalRepository signals, TimeProvider clock) : IHandler
{
    public async Task<Result> Handle(Guid signalId, CancellationToken cancellationToken)
    {
        var signal = await signals.FindAsync(signalId, cancellationToken);
        if (signal is null)
            return SignalErrors.NotFound(signalId);

        var openAlarms = await signals.OpenOccurrencesOfAsync(signalId, cancellationToken);

        var deleted = signal.Delete(openAlarms, clock.GetUtcNow());
        if (deleted.IsFailure)
            return deleted;

        signals.Remove(signal);
        await signals.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
