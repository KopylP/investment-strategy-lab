namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record JournalPageResult(int Total, int Offset, IReadOnlyList<ReplayJournalEntry> Entries);
