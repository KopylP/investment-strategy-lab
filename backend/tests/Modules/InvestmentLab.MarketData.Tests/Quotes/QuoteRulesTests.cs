using InvestmentLab.MarketData.Instruments.Domain;
using InvestmentLab.MarketData.Quotes.Domain;

namespace InvestmentLab.MarketData.Tests.Quotes;

public sealed class QuoteRulesTests
{
    private static readonly InstrumentId Sp500 = InstrumentId.New();
    private static readonly DateTimeOffset FetchedAt = new(2026, 7, 20, 20, 30, 0, TimeSpan.Zero);

    private static DailyQuote Quote(int month, int day, decimal adjustedClose) =>
        DailyQuote.Fetched(Sp500, new DateOnly(2026, month, day), adjustedClose + 1m, adjustedClose, "yfinance", FetchedAt);

    [Fact]
    public void A_normal_day_has_no_flags()
    {
        var quotes = new[] { Quote(7, 14, 100m), Quote(7, 15, 101m), Quote(7, 16, 118m) };

        QuoteQualityInspector.Inspect(quotes, previous: null);

        quotes.ShouldAllBe(quote => quote.QualityFlags == QualityFlags.None);
    }

    [Fact]
    public void A_move_of_more_than_twenty_percent_in_a_day_is_a_spike()
    {
        var quotes = new[] { Quote(7, 15, 100m), Quote(7, 16, 79m), Quote(7, 17, 90m) };

        QuoteQualityInspector.Inspect(quotes, previous: null);

        quotes[1].QualityFlags.ShouldBe(QualityFlags.Spike);
        quotes[2].QualityFlags.ShouldBe(QualityFlags.None);
    }

    [Fact]
    public void The_first_new_quote_is_compared_with_the_last_stored_one()
    {
        var quotes = new[] { Quote(7, 16, 130m) };

        QuoteQualityInspector.Inspect(quotes, previous: Quote(7, 15, 100m));

        quotes[0].QualityFlags.ShouldBe(QualityFlags.Spike);
    }

    [Fact]
    public void More_than_five_missing_business_days_is_a_gap()
    {
        var noGap = new[] { Quote(7, 1, 100m), Quote(7, 9, 100m) };
        var gap = new[] { Quote(7, 1, 100m), Quote(7, 10, 100m) };

        QuoteQualityInspector.Inspect(noGap, previous: null);
        QuoteQualityInspector.Inspect(gap, previous: null);

        noGap[1].QualityFlags.ShouldBe(QualityFlags.None);
        gap[1].QualityFlags.ShouldBe(QualityFlags.Gap);
    }

    [Fact]
    public void A_weekend_is_not_a_gap()
    {
        var quotes = new[] { Quote(7, 17, 100m), Quote(7, 20, 100m) };

        QuoteQualityInspector.Inspect(quotes, previous: null);

        quotes[1].QualityFlags.ShouldBe(QualityFlags.None);
    }

    [Theory]
    [InlineData(2026, 7, 13, false)]
    [InlineData(2026, 7, 10, true)]
    public void A_last_quote_older_than_five_business_days_is_stale(int year, int month, int day, bool stale)
    {
        QuoteQualityInspector.IsStale(new DateOnly(year, month, day), asOf: new DateOnly(2026, 7, 20)).ShouldBe(stale);
    }

    [Fact]
    public void A_corrected_quote_is_not_overwritten_by_a_later_fetch()
    {
        var quote = Quote(7, 15, 100m);
        quote.ApplyCorrection(close: 102m, adjustedClose: 101m);

        var refreshed = quote.Refresh(close: 50m, adjustedClose: 50m, "yfinance", FetchedAt.AddDays(1));

        refreshed.ShouldBeFalse();
        quote.Close.ShouldBe(102m);
        quote.AdjustedClose.ShouldBe(101m);
        quote.QualityFlags.ShouldBe(QualityFlags.Corrected);
    }

    [Fact]
    public void An_uncorrected_quote_takes_the_fresh_values()
    {
        var quote = Quote(7, 15, 100m);

        quote.Refresh(close: 103m, adjustedClose: 102m, "yfinance", FetchedAt.AddDays(1)).ShouldBeTrue();

        quote.AdjustedClose.ShouldBe(102m);
    }

    [Fact]
    public void Fetching_the_same_values_again_changes_nothing()
    {
        var quote = Quote(7, 15, 100m);

        quote.Refresh(close: 101m, adjustedClose: 100m, "yfinance", FetchedAt.AddDays(1)).ShouldBeFalse();

        quote.FetchedAt.ShouldBe(FetchedAt);
    }
}
