using InvestmentLab.Signals.Monitoring.Application.Shared;
using InvestmentLab.Signals.Monitoring.Domain;

namespace InvestmentLab.Signals.Monitoring.Application.ChangeSignal;

public sealed record ChangeSignalCommand(string Name, SignalConditionDto Condition, SignalScheduleDto Schedule, bool Enabled);

public sealed class ChangeSignalHandler(ISignalRepository signals, TimeProvider clock)
{
    public async Task<Result> Handle(Guid signalId, ChangeSignalCommand command, CancellationToken cancellationToken)
    {
        var signal = await signals.FindAsync(signalId, cancellationToken);
        if (signal is null)
            return SignalErrors.NotFound(signalId);

        var settings = SettingsOf(command);
        if (settings.IsFailure)
            return settings.Error;

        var openAlarms = await signals.OpenOccurrencesOfAsync(signalId, cancellationToken);

        var changed = signal.Change(settings.Value, openAlarms, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed;

        await signals.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private static Result<SignalSettings> SettingsOf(ChangeSignalCommand command)
    {
        var condition = command.Condition.ToCondition();
        if (condition.IsFailure)
            return condition.Error;

        var schedule = command.Schedule.ToCadence();
        if (schedule.IsFailure)
            return schedule.Error;

        return new SignalSettings(command.Name, condition.Value, schedule.Value, command.Enabled);
    }
}
