namespace InvestmentLab.Rebalancing.Planning.Domain;

public sealed class PlanSettings : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public decimal MonthlyContribution { get; private set; }
    public decimal MinTrade { get; private set; }
    public Percentage TrimBand { get; private set; }
    public bool AllowFractionalShares { get; private set; }

    public bool GenerateOnSchedule { get; private set; }

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

    public OwnReportSchedule? ScheduleOf(string code) => ReportSchedules.FirstOrDefault(own => own.Code == code);

    public void Schedule(string code, Cadence cadence, DateTimeOffset now)
    {
        ReportSchedules = [.. ReportSchedules.Where(own => own.Code != code), new OwnReportSchedule(code, cadence, now)];
        UpdatedAt = now;
    }

    public void UseStrategySchedule(string code, DateTimeOffset now)
    {
        ReportSchedules = [.. ReportSchedules.Where(own => own.Code != code)];
        UpdatedAt = now;
    }
}
