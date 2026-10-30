using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule);

/// <param name="TrimBand">Fraction 0..1.</param>
public sealed record ChangePlanSettingsCommand(
    decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule = true);

/// <summary>One of the reports the account's strategies declare, at one frequency (strategies of the same type may set different ones).</summary>
/// <param name="Mode">"rebalance" or "riskReview".</param>
/// <param name="Frequency">Off, Weekly, Monthly or Quarterly.</param>
/// <param name="Time">HH:mm on the report's Sunday.</param>
/// <param name="Next">Null when off: made only on demand.</param>
/// <param name="Strategies">"Sub-portfolio: strategy" pairs it covers.</param>
public sealed record ScheduledReportDto(
    string Code, string Name, string Description, string Mode, string Frequency, string Time, DateOnly? Next, IReadOnlyList<string> Strategies);

public sealed record ReportScheduleDto(bool GenerateOnSchedule, string TimeZone, IReadOnlyList<ScheduledReportDto> Reports);

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

    /// <summary>Which reports the account's strategies declare and when the next ones come (the strategies decide, not the account).</summary>
    public async Task<Result<ReportScheduleDto>> Schedule(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var today = ReportSchedules.Today(clock);
        var reports = (await inputs.ReportsAsync(account, cancellationToken))
            .SelectMany(report => report.SubPortfolios.GroupBy(sub => sub.Cadence).Select(group => new ScheduledReportDto(
                report.Code, report.Name, report.Description, report.Mode, group.Key.Frequency.ToString(), group.Key.Time.ToString("HH:mm"),
                group.Key.Next(today), [.. group.Select(sub => $"{sub.SubPortfolioName}: {sub.StrategyName}")])));

        var current = await settings.FindAsync(accountId, cancellationToken);
        return new ReportScheduleDto(current?.GenerateOnSchedule ?? true, ReportSchedules.TimeZone, [.. reports]);
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
