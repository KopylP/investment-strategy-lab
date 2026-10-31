using InvestmentLab.Rebalancing.Planning.Application.GetReportSchedule;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application.Shared;

public sealed class OwnReportScheduleChange(
    PlanningInputs inputs, IPlanSettingsRepository settings, GetReportScheduleHandler schedule, TimeProvider clock)
{
    public async Task<Result<ReportScheduleDto>> ApplyAsync(
        Guid accountId, string code, Action<PlanSettings, DateTimeOffset> change, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var reports = await inputs.ReportsAsync(account, null, cancellationToken);
        if (reports.All(report => report.Code != code))
            return Error.NotFound("report.unknown_code", $"No strategy of account {accountId} makes report '{code}'.");

        var current = await FindOrAddAsync(accountId, UserId.Of(account.OwnerId), cancellationToken);

        change(current, clock.GetUtcNow());
        await settings.SaveChangesAsync(cancellationToken);

        return await schedule.Handle(accountId, cancellationToken);
    }

    private async Task<PlanSettings> FindOrAddAsync(Guid accountId, UserId ownerId, CancellationToken cancellationToken)
    {
        var existing = await settings.FindAsync(accountId, cancellationToken);
        if (existing is not null)
            return existing;

        var created = PlanSettings.DefaultFor(accountId, ownerId);
        settings.Add(created);

        return created;
    }
}
