using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Tests.Experiments;

public sealed class ExperimentJournalTests
{
    private static readonly DateOnly Day = new(2025, 1, 2);

    [Fact]
    public void Filters_are_applied_before_counting_and_paging()
    {
        var journal = new ReplayJournal([
            new ReplayJournalEntry(Day, "buy", "first"),
            new ReplayJournalEntry(Day.AddDays(1), "sell", "sale"),
            new ReplayJournalEntry(Day.AddDays(2), "buy", "second"),
            new ReplayJournalEntry(Day.AddDays(3), "buy", "third")]);
        var filter = new JournalFilter(Kind: "buy", From: Day, To: Day.AddDays(2));

        var page = journal.Page(filter, new JournalPage(Offset: 1, Limit: 1));

        page.Total.ShouldBe(2);
        page.Entries.ShouldHaveSingleItem().Reason.ShouldBe("second");
    }

    [Fact]
    public void Pagination_bounds_prevent_unbounded_or_negative_requests()
    {
        var page = new JournalPage(Offset: -1, Limit: 10000);

        page.FirstEntry.ShouldBe(0);
        page.PageSize.ShouldBe(200);
        new JournalPage(Limit: 0).PageSize.ShouldBe(1);
    }
}
