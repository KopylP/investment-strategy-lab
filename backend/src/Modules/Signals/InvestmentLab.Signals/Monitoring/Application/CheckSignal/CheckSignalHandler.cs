using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.CheckSignal;

public sealed class CheckSignalHandler(ISignalRepository signals, SignalChecker checker)
{
    public async Task<Result> Handle(Guid signalId, CancellationToken cancellationToken)
    {
        var signal = await signals.FindAsync(signalId, cancellationToken);
        if (signal is null)
            return SignalErrors.NotFound(signalId);

        await checker.CheckAsync(signal, cancellationToken);

        return Result.Success();
    }
}
