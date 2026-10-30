using System.Globalization;
using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule);

/// <param name="TrimBand">Fraction 0..1.</param>
public sealed record ChangePlanSettingsCommand(
    decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule = true);

/// <summary>When a report comes, in <see cref="ReportScheduleDto.TimeZone"/>; each frequency reads only its own fields.</summary>
/// <param name="Frequency">Off, Hourly, Daily, Weekly, Monthly or Quarterly.</param>
/// <param name="Day">Monthly: day of the month; quarterly: of the quarter's first month (a shorter month: its last day).</param>
/// <param name="Weekday">Weekly: Monday … Sunday.</param>
/// <param name="Time">Daily, weekly, monthly, quarterly: HH:mm.</param>
/// <param name="Minute">Hourly: the minute of every hour.</param>
public sealed record ReportTimingDto(string Frequency, int Day, string Weekday, string Time, int Minute)
{
    public static ReportTimingDto Of(ReportCadence cadence) => new(
        cadence.Frequency.ToString(), cadence.Day, cadence.Weekday.ToString(), cadence.Time.ToString("HH:mm", CultureInfo.InvariantCulture), cadence.Minute);
}

/// <summary>One of the reports the account's strategies declare, at one schedule (strategies of the same type may declare different ones).</summary>
/// <param name="Mode">"rebalance" or "riskReview".</param>
/// <param name="Schedule">When it comes: the user's own time if <paramref name="Own"/>, else the strategy's.</param>
/// <param name="StrategySchedule">When its strategy says it comes (the first sub-portfolio's, when they differ).</param>
/// <param name="Own">The user set the time for the account; then one entry covers every sub-portfolio.</param>
/// <param name="Next">Local date and time of the next one (yyyy-MM-ddTHH:mm in <see cref="ReportScheduleDto.TimeZone"/>); null when off: made only on demand.</param>
/// <param name="Strategies">"Sub-portfolio: strategy" pairs it covers.</param>
public sealed record ScheduledReportDto(
    string Code, string Name, string Description, string Mode, ReportTimingDto Schedule, ReportTimingDto StrategySchedule, bool Own, string? Next,
    IReadOnlyList<string> Strategies);

public sealed record ReportScheduleDto(bool GenerateOnSchedule, string TimeZone, IReadOnlyList<ScheduledReportDto> Reports);

/// <summary>The user's own time for one of the account's reports (<see cref="ReportCadence.TryOf"/>).</summary>
/// <param name="Frequency">off, hourly, daily, weekly, monthly or quarterly.</param>
/// <param name="Day">Monthly, quarterly: 1–31.</param>
/// <param name="Weekday">Weekly: monday … sunday.</param>
/// <param name="Time">Daily, weekly, monthly, quarterly: HH:mm.</param>
/// <param name="Minute">Hourly: 0–59.</param>
public sealed record ChangeReportScheduleCommand(string Frequency, int? Day = null, string? Weekday = null, string? Time = null, int? Minute = null);

/// <summary>Read and change how an account's plan is made; an account without settings gets the defaults.</summary>
public sealed class PlanSettingsHandler(PlanningInputs inputs, IPlanSettingsRepository settings, TimeProvider clock)
{
    public async Task<Result<PlanSettingsDto>> Get(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken) ?? PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId));
        return ToDto(current);
    }

    /// <summary>Which reports the account's strategies declare and when the next ones come: at the user's time if set, else the strategy's.</summary>
    public async Task<Result<ReportScheduleDto>> Schedule(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var current = await settings.FindAsync(accountId, cancellationToken);
        var now = ReportSchedules.Local(clock.GetUtcNow()).DateTime;
        var reports = (await inputs.ReportsAsync(account, current, cancellationToken))
            .SelectMany(report => report.SubPortfolios.GroupBy(report.CadenceOf).Select(group => new ScheduledReportDto(
                report.Code, report.Name, report.Description, report.Mode, ReportTimingDto.Of(group.Key), ReportTimingDto.Of(group.First().Default),
                report.Own is not null, group.Key.Next(now)?.ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture),
                [.. group.Select(sub => $"{sub.SubPortfolioName}: {sub.StrategyName}")])));

        return new ReportScheduleDto(current?.GenerateOnSchedule ?? true, ReportSchedules.TimeZone, [.. reports]);
    }

    /// <summary>The report with this code comes at the user's time on this account, whatever its strategies say.</summary>
    public async Task<Result<ReportScheduleDto>> ChangeSchedule(Guid accountId, string code, ChangeReportScheduleCommand command, CancellationToken cancellationToken)
    {
        var cadence = ReportCadence.TryOf(command.Frequency, command.Day, command.Weekday, command.Time, command.Minute);
        if (cadence.IsFailure)
            return cadence.Error;
        return await ChangeOwnScheduleAsync(accountId, code, (plan, now) => plan.Schedule(code, cadence.Value, now), cancellationToken);
    }

    /// <summary>The report with this code comes when its strategies say again.</summary>
    public Task<Result<ReportScheduleDto>> UseStrategySchedule(Guid accountId, string code, CancellationToken cancellationToken) =>
        ChangeOwnScheduleAsync(accountId, code, (plan, now) => plan.UseStrategySchedule(code, now), cancellationToken);

    private async Task<Result<ReportScheduleDto>> ChangeOwnScheduleAsync(
        Guid accountId, string code, Action<PlanSettings, DateTimeOffset> change, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");
        if ((await inputs.ReportsAsync(account, null, cancellationToken)).All(report => report.Code != code))
            return Error.NotFound("report.unknown_code", $"No strategy of account {accountId} makes report '{code}'.");

        var current = await settings.FindAsync(accountId, cancellationToken);
        if (current is null)
        {
            current = PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId));
            settings.Add(current);
        }
        change(current, clock.GetUtcNow());
        await settings.SaveChangesAsync(cancellationToken);
        return await Schedule(accountId, cancellationToken);
    }

    public async Task<Result<PlanSettingsDto>> Change(Guid accountId, ChangePlanSettingsCommand command, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var policy = RebalancingPolicy.TryOf(command.MinTrade, command.TrimBand, command.AllowFractionalShares);
        if (policy.IsFailure)
            return policy.Error;

        var current = await settings.FindAsync(accountId, cancellationToken);
        if (current is null)
        {
            current = PlanSettings.DefaultFor(accountId, UserId.Of(account.OwnerId));
            settings.Add(current);
        }

        var changed = current.Change(command.MonthlyContribution, policy.Value, command.GenerateOnSchedule, clock.GetUtcNow());
        if (changed.IsFailure)
            return changed.Error;

        await settings.SaveChangesAsync(cancellationToken);
        return ToDto(current);
    }

    private static PlanSettingsDto ToDto(PlanSettings settings) => new(
        settings.AccountId, settings.MonthlyContribution, settings.MinTrade, settings.TrimBand.Value, settings.AllowFractionalShares, settings.GenerateOnSchedule);
}
