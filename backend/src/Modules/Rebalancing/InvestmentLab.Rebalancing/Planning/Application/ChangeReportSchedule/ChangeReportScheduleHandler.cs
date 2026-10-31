using InvestmentLab.Rebalancing.Planning.Application.Shared;

namespace InvestmentLab.Rebalancing.Planning.Application.ChangeReportSchedule;

public sealed record ChangeReportScheduleCommand(string Frequency, int? Day = null, string? Weekday = null, string? Time = null, int? Minute = null);

public sealed class ChangeReportScheduleHandler(OwnReportScheduleChange ownSchedule)
{
    public async Task<Result<ReportScheduleDto>> Handle(Guid accountId, string code, ChangeReportScheduleCommand command, CancellationToken cancellationToken)
    {
        var cadence = Cadence.TryOf(command.Frequency, command.Day, command.Weekday, command.Time, command.Minute);
        if (cadence.IsFailure)
            return cadence.Error;

        return await ownSchedule.ApplyAsync(accountId, code, (plan, now) => plan.Schedule(code, cadence.Value, now), cancellationToken);
    }
}
