namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>
/// How an account's monthly plan is made: the usual contribution, the rebalancing policy and when the reports come.
/// One per account; without it the defaults apply (no contribution, min trade 25, trim band 10%, fractional shares,
/// every report when its strategy says).
/// </summary>
public sealed class PlanSettings : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public decimal MonthlyContribution { get; private set; }
    public decimal MinTrade { get; private set; }
    public Percentage TrimBand { get; private set; }
    public bool AllowFractionalShares { get; private set; }

    /// <summary>The scheduled job generates the strategies' reports for this account.</summary>
    public bool GenerateOnSchedule { get; private set; }

    /// <summary>The user's own times for the strategies' reports (by report code); a report not listed comes when its strategy says.</summary>
    public List<OwnReportSchedule> ReportSchedules { get; private set; } = [];

    public DateTimeOffset UpdatedAt { get; private set; }

    private PlanSettings(Guid accountId, UserId ownerId) : base(accountId)
    {
        OwnerId = ownerId;
        TrimBand = RebalancingPolicy.DefaultTrimBand;
        MinTrade = RebalancingPolicy.DefaultMinTrade;
        AllowFractionalShares = true;
        GenerateOnSchedule = true;
    }

    // EF Core.
    private PlanSettings(Guid id) : base(id) => TrimBand = null!;

    public static PlanSettings DefaultFor(Guid accountId, UserId ownerId) => new(accountId, ownerId);

    public Guid AccountId => Id;

    public RebalancingPolicy Policy => RebalancingPolicy.Of(MinTrade, TrimBand, AllowFractionalShares);

    public Result Change(decimal monthlyContribution, RebalancingPolicy policy, bool generateOnSchedule, DateTimeOffset now)
    {
        if (monthlyContribution < 0m)
            return Error.Validation("plan_settings.contribution_negative", "Monthly contribution cannot be negative.");

        MonthlyContribution = decimal.Round(monthlyContribution, 2);
        MinTrade = policy.MinTrade;
        TrimBand = policy.TrimBand;
        AllowFractionalShares = policy.AllowFractionalShares;
        GenerateOnSchedule = generateOnSchedule;
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>The user's own schedule of the report with this code; null — when its strategy says.</summary>
    public OwnReportSchedule? ScheduleOf(string code) => ReportSchedules.FirstOrDefault(own => own.Code == code);

    /// <summary>The report with this code comes at the user's time from now on, whatever its strategy says.</summary>
    public void Schedule(string code, Cadence cadence, DateTimeOffset now)
    {
        ReportSchedules = [.. ReportSchedules.Where(own => own.Code != code), new OwnReportSchedule(code, cadence, now)];
        UpdatedAt = now;
    }

    /// <summary>The report with this code comes when its strategy says again.</summary>
    public void UseStrategySchedule(string code, DateTimeOffset now)
    {
        ReportSchedules = [.. ReportSchedules.Where(own => own.Code != code)];
        UpdatedAt = now;
    }
}

/// <param name="Code">The strategies' report code, e.g. "rebalance".</param>
/// <param name="Since">When the user set it: report times before it do not count (no report the moment a time is set).</param>
public sealed record OwnReportSchedule(string Code, Cadence Cadence, DateTimeOffset Since);
