namespace InvestmentLab.Rebalancing.Planning.Domain;

/// <summary>
/// How an account's monthly plan is made: the usual contribution and the rebalancing policy. One per account;
/// without it the defaults apply (no contribution, min trade 25, trim band 10%, fractional shares).
/// </summary>
public sealed class PlanSettings : AggregateRoot<Guid>
{
    public UserId OwnerId { get; private set; }
    public decimal MonthlyContribution { get; private set; }
    public decimal MinTrade { get; private set; }
    public Percentage TrimBand { get; private set; }
    public bool AllowFractionalShares { get; private set; }

    /// <summary>The scheduled job generates the monthly report and the weekly risk review for this account.</summary>
    public bool GenerateOnSchedule { get; private set; }

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
}
