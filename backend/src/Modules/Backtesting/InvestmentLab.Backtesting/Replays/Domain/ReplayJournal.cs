namespace InvestmentLab.Backtesting.Replays.Domain;

public sealed class ReplayJournal(IReadOnlyList<ReplayJournalEntry>? entries = null)
{
    private readonly List<ReplayJournalEntry> _entries = [.. entries ?? []];

    public IReadOnlyList<ReplayJournalEntry> Entries => _entries;

    public void Add(ReplayJournalEntry entry) => _entries.Add(entry);

    public JournalPageResult Page(JournalFilter filter, JournalPage page)
    {
        var matching = _entries.Where(filter.Matches).ToList();
        var selected = matching.Skip(page.FirstEntry).Take(page.PageSize).ToList();

        return new JournalPageResult(
            Total: matching.Count,
            Offset: page.FirstEntry,
            Entries: selected);
    }
}
