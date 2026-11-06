namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record JournalPage(int Offset = 0, int Limit = 50)
{
    private const int MaximumPageSize = 200;

    public int FirstEntry => Math.Max(0, Offset);
    public int PageSize => Math.Clamp(Limit, 1, MaximumPageSize);
}
