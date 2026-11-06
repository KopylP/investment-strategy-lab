namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record PendingReplayOrder(ReplayTrade Trade, DateOnly PlacedOn, string Reason, bool IsRiskExit = false)
{
    private const int MaximumAgeDays = 7;

    public bool HasExpired(DateOnly day) => day.DayNumber - PlacedOn.DayNumber > MaximumAgeDays;
}
