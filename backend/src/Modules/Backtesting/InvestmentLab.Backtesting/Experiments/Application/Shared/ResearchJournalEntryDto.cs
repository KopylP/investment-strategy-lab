using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchJournalEntryDto(
    DateOnly Date,
    string Kind,
    string Reason,
    ResearchFillDto? Fill,
    ResearchAllocationDto? Allocation,
    ResearchExplanationDto? Explanation,
    bool? MarketFavorable)
{
    public static ResearchJournalEntryDto From(ReplayJournalEntry replayJournalEntry) => new(
        Date: replayJournalEntry.Date,
        Kind: replayJournalEntry.Kind,
        Reason: replayJournalEntry.Reason,
        Fill: replayJournalEntry.Fill is { } fill ? ResearchFillDto.From(fill) : null,
        Allocation: replayJournalEntry.Allocation is { } allocation ? ResearchAllocationDto.From(allocation) : null,
        Explanation: replayJournalEntry.Explanation is { } explanation ? ResearchExplanationDto.From(explanation) : null,
        MarketFavorable: replayJournalEntry.MarketFavorable);
}
