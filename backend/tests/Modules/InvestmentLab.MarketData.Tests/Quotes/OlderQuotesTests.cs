using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Tests.Quotes;

public sealed class OlderQuotesTests
{
    private static readonly DateOnly FirstStoredDay = new(2023, 11, 1);

    [Fact]
    public void Only_prices_before_the_first_stored_day_are_taken_from_the_archive()
    {
        ArchivedPrice[] archived = [PriceOn(-2, 50m), PriceOn(-1, 51m), PriceOn(0, 52m), PriceOn(1, 53m)];

        var older = OlderQuotes.Before(archived, new FirstStoredQuote(FirstStoredDay, 52m));

        older.Select(price => price.Date).ShouldBe([FirstStoredDay.AddDays(-2), FirstStoredDay.AddDays(-1)]);
    }

    [Fact]
    public void Adjusted_prices_are_scaled_to_meet_the_stored_adjusted_price_so_the_seam_shows_no_false_return()
    {
        ArchivedPrice[] archived = [PriceOn(-1, 40m), PriceOn(0, 50m)];

        var older = OlderQuotes.Before(archived, new FirstStoredQuote(FirstStoredDay, 45m));

        older.Single().AdjustedClose.ShouldBe(36m);
        older.Single().Close.ShouldBe(40m);
    }

    [Fact]
    public void Without_stored_quotes_the_whole_archive_is_older()
    {
        ArchivedPrice[] archived = [PriceOn(-1, 40m), PriceOn(0, 50m)];

        OlderQuotes.Before(archived, firstStored: null).ShouldBe(archived);
    }

    [Fact]
    public void An_empty_archive_adds_nothing()
    {
        ArchivedPrice[] archived = [];

        OlderQuotes.Before(archived, new FirstStoredQuote(FirstStoredDay, 45m)).ShouldBeEmpty();
    }

    private static ArchivedPrice PriceOn(int daysFromFirstStored, decimal price) =>
        new(FirstStoredDay.AddDays(daysFromFirstStored), price, price);
}
