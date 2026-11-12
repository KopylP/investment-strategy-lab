using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Replays.Application.Shared;

public sealed record ReplayJournalEntryDto(
    DateOnly Date,
    string Kind,
    string Reason,
    ReplayFillDto? Fill,
    ReplayAllocationDto? Allocation,
    ReplayExplanationDto? Explanation,
    bool? MarketFavorable)
{
    public static ReplayJournalEntryDto From(ReplayJournalEntry replayJournalEntry) => new(
        Date: replayJournalEntry.Date,
        Kind: replayJournalEntry.Kind,
        Reason: replayJournalEntry.Reason,
        Fill: replayJournalEntry.Fill is { } fill ? ReplayFillDto.From(fill) : null,
        Allocation: replayJournalEntry.Allocation is { } allocation ? ReplayAllocationDto.From(allocation) : null,
        Explanation: replayJournalEntry.Explanation is { } explanation ? ReplayExplanationDto.From(explanation) : null,
        MarketFavorable: replayJournalEntry.MarketFavorable);
}
