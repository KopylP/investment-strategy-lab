namespace InvestmentLab.BuildingBlocks.Messaging;

public static class OutboxRetryDelay
{
    private static readonly TimeSpan FirstDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LongestDelay = TimeSpan.FromHours(1);
    private static readonly int DoublingsToReachLongestDelay = (int)Math.Ceiling(Math.Log2(LongestDelay / FirstDelay));

    public static TimeSpan After(int failures)
    {
        var doublings = Math.Clamp(failures - 1, 0, DoublingsToReachLongestDelay);
        var delay = FirstDelay * Math.Pow(2, doublings);

        return delay < LongestDelay ? delay : LongestDelay;
    }
}
