using InvestmentLab.Signals.Monitoring.Application.ReadSubjects;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.CheckSignal;

public sealed class SignalChecker(ISignalRepository signals, SignalReader reader, TimeProvider clock)
{
    public async Task CheckAsync(Signal signal, CancellationToken cancellationToken)
    {
        var openAlarms = await signals.OpenOccurrencesOfAsync(signal.Id, cancellationToken);
        var readings = await reader.ReadAsync(signal, cancellationToken);

        var outcome = signal.Check(openAlarms, readings, clock.GetUtcNow());
        foreach (var alarm in outcome.Fired)
            signals.Add(alarm.Occurrence);

        await signals.SaveChangesAsync(cancellationToken);
    }
}
