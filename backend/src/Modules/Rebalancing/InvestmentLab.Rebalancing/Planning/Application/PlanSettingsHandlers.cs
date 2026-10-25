using InvestmentLab.Rebalancing.Planning.Domain;

namespace InvestmentLab.Rebalancing.Planning.Application;

public sealed record PlanSettingsDto(
    Guid AccountId, decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule);

/// <param name="TrimBand">Fraction 0..1.</param>
public sealed record ChangePlanSettingsCommand(
    decimal MonthlyContribution, decimal MinTrade, decimal TrimBand, bool AllowFractionalShares, bool GenerateOnSchedule = true);

/// <param name="Kind">Rebalance or RiskReview.</param>
/// <param name="Frequency">Weekly, Monthly or Quarterly.</param>
public sealed record ScheduledReportDto(string Kind, string Frequency, DateOnly Next, IReadOnlyList<string> Strategies);

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

    /// <summary>Which reports the account's strategies ask for and when the next ones come (the strategies decide, not the account).</summary>
    public async Task<Result<ReportScheduleDto>> Schedule(Guid accountId, CancellationToken cancellationToken)
    {
        if (await inputs.AccountAsync(accountId, cancellationToken) is not { } account)
            return Error.NotFound("account.not_found", $"Account {accountId} does not exist.");

        var today = ReportSchedules.Today(clock);
        var cadences = await inputs.CadencesAsync(account, cancellationToken);
        var rebalances = cadences.GroupBy(each => each.Cadence.Rebalance)
            .Select(group => new ScheduledReportDto("Rebalance", group.Key.ToString(), group.First().Cadence.NextRebalance(today),
                [.. group.Select(each => $"{each.SubPortfolioName}: {each.StrategyName}")]));
        var reviews = cadences.Where(each => each.Cadence.WeeklyRiskReview).ToList();
        IEnumerable<ScheduledReportDto> risk = reviews.Count == 0
            ? []
            : [new ScheduledReportDto("RiskReview", "Weekly", reviews[0].Cadence.NextRiskReview(today)!.Value, [.. reviews.Select(each => $"{each.SubPortfolioName}: {each.StrategyName}")])];

        var current = await settings.FindAsync(accountId, cancellationToken);
        return new ReportScheduleDto(current?.GenerateOnSchedule ?? true, ReportSchedules.TimeZone, [.. rebalances.OrderBy(report => report.Next), .. risk]);
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
