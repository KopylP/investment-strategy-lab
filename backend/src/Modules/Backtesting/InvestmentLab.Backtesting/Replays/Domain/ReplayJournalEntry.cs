namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record ReplayJournalEntry(
    DateOnly Date,
    string Kind,
    string Reason,
    ReplayFill? Fill = null,
    ReplayAllocation? Allocation = null,
    ReplayExplanation? Explanation = null,
    bool? MarketFavorable = null)
{
    public ReplayJournalEntry WithoutCandidates() =>
        Explanation is null ? this : this with { Explanation = Explanation with { Candidates = null } };
}
