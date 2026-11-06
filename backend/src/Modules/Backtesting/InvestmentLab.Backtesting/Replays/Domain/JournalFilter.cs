namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed record JournalFilter(string? Kind = null, DateOnly? From = null, DateOnly? To = null)
{
    public bool Matches(ReplayJournalEntry entry)
    {
        if (!string.IsNullOrEmpty(Kind) && entry.Kind != Kind)
            return false;

        if (From is { } from && entry.Date < from)
            return false;

        return To is not { } to || entry.Date <= to;
    }
}
