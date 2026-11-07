namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record FundsHistoryLimit(int Warmed, int Needed, IReadOnlyList<Guid> NotWarmed) : HistoryLimit
{
    public override string Describe(Func<Guid, string> labelOf)
    {
        var names = NotWarmed.Select(labelOf).ToList();

        return Message.Of("research.history_limit.funds", new { warmed = Warmed, needed = Needed, names });
    }
}
