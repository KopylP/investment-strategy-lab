using InvestmentLab.Portfolio.Transactions.Domain;
using InvestmentLab.Portfolio.Transactions.Infrastructure;

namespace InvestmentLab.Portfolio.Tests.Transactions;

public sealed class StatementImportTests
{
    internal const string Statement = """
        Date,Ticker,Type,Quantity,Price per share,Total Amount,Currency,FX Rate
        2026-06-01T09:00:00.100Z,,CASH TOP-UP,,,EUR 500,EUR,0.2340
        2026-06-02T10:15:30.200Z,ETF,BUY - MARKET,2,EUR 100,EUR 200,EUR,0.2340
        2026-06-03T11:00:00.300Z,ETF,BUY - MARKET,1,EUR 110,EUR 110,EUR,0.2340
        2026-06-10T12:00:00.400Z,ETF,DIVIDEND,,,EUR 1.50,EUR,0.2341
        2026-06-15T13:00:00.500Z,ETF,SELL - MARKET,1,EUR 120,EUR 120,EUR,0.2342
        2026-06-20T14:00:00.600Z,,CASH WITHDRAWAL,,,"EUR -1,000.25",EUR,0.2343
        2026-06-21T15:00:00.700Z,ETF,STOCK SPLIT,2,,,EUR,0.2343
        """;

    private static IReadOnlyList<StatementRow> Rows() => new RevolutStatementParser().Parse(Statement).Value.Rows;

    [Fact]
    public void Revolut_rows_are_read_with_amounts_currencies_and_stable_identities()
    {
        var rows = Rows();

        rows.Select(row => row.Kind).ShouldBe([
            StatementRowKind.Contribution, StatementRowKind.Buy, StatementRowKind.Buy, StatementRowKind.Dividend,
            StatementRowKind.Sell, StatementRowKind.Withdrawal, StatementRowKind.Unsupported]);
        rows[1].ShouldSatisfyAllConditions(
            row => row.Quantity.ShouldBe(2m),
            row => row.Price.ShouldBe(100m),
            row => row.Amount.ShouldBe(200m),
            row => row.Currency.ShouldBe("EUR"),
            row => row.OccurredOn.ShouldBe(new DateOnly(2026, 6, 2)));
        rows[5].Amount.ShouldBe(1000.25m);
        rows.Select(row => row.Identity).Distinct().Count().ShouldBe(rows.Count);
        Rows().Select(row => row.Identity).ShouldBe(rows.Select(row => row.Identity));
    }

    [Fact]
    public void Not_a_statement_is_refused()
    {
        new RevolutStatementParser().Parse("a,b,c\n1,2,3").Error.Code.ShouldBe("statement_import.unknown_format");
    }

    [Fact]
    public void Merge_records_new_rows_skips_imported_ones_and_matches_the_same_trade_marked_in_a_report()
    {
        var fixture = new PortfolioFixture();
        var rows = Priced(fixture);

        fixture.Add(Transaction.Trade(fixture.Account, TradeSide.Buy, PortfolioFixture.Etf, Quantity.Of(2m), Price.Of(100m, Currency.EUR), Money.Zero(Currency.EUR),
            [new Allocation(fixture.A, Quantity.Of(2m))], new DateOnly(2026, 6, 2), new TransactionSource(TransactionSourceKind.Import, rows[1].Row.Identity), DateTimeOffset.UtcNow));
        var marked = fixture.Add(Transaction.Trade(fixture.Account, TradeSide.Buy, PortfolioFixture.Etf, Quantity.Of(1m), Price.Of(109m, Currency.EUR), Money.Zero(Currency.EUR),
            [new Allocation(fixture.A, Quantity.Of(1m))], new DateOnly(2026, 6, 4), new TransactionSource(TransactionSourceKind.ReportAction, "rebalancing:x"), DateTimeOffset.UtcNow));

        var outcomes = StatementMatcher.Match(rows, fixture.Journal, ImportMode.Merge, null);

        outcomes.Select(outcome => outcome.Decision).ShouldBe([
            RowDecision.New, RowDecision.AlreadyImported, RowDecision.MatchesExisting, RowDecision.New, RowDecision.New, RowDecision.New, RowDecision.Unsupported]);
        var corrected = outcomes[2];
        (corrected.ExistingTransactionId, corrected.WillReverseExisting, corrected.WillRecord).ShouldBe((marked.Id.Value, true, true));
        outcomes.Count(outcome => outcome.WillRecord).ShouldBe(5);
    }

    [Fact]
    public void Changed_rows_are_rewritten_only_when_asked_and_replace_reverses_earlier_imports()
    {
        var fixture = new PortfolioFixture();
        var rows = Priced(fixture);
        var earlier = fixture.Add(Transaction.Trade(fixture.Account, TradeSide.Buy, PortfolioFixture.Etf, Quantity.Of(2m), Price.Of(99m, Currency.EUR), Money.Zero(Currency.EUR),
            [new Allocation(fixture.A, Quantity.Of(2m))], new DateOnly(2026, 6, 2), new TransactionSource(TransactionSourceKind.Import, rows[1].Row.Identity), DateTimeOffset.UtcNow));
        var manual = fixture.Contribute(fixture.B, 50m, 1);

        StatementMatcher.Match(rows, fixture.Journal, ImportMode.Merge, null)[1].ShouldSatisfyAllConditions(
            outcome => outcome.Decision.ShouldBe(RowDecision.Differs),
            outcome => outcome.WillRecord.ShouldBeFalse());
        StatementMatcher.Match(rows, fixture.Journal, ImportMode.UpdateImported, null)[1].ShouldSatisfyAllConditions(
            outcome => outcome.WillReverseExisting.ShouldBeTrue(),
            outcome => outcome.WillRecord.ShouldBeTrue());

        StatementMatcher.ToReverse(fixture.Journal, ImportMode.Replace).ShouldBe([earlier]);
        StatementMatcher.ToReverse(fixture.Journal, ImportMode.FromScratch).Count.ShouldBe(2);
        StatementMatcher.Match(rows, fixture.Journal, ImportMode.Merge, new DateOnly(2026, 6, 10)).Count(o => o.Decision == RowDecision.OutOfScope).ShouldBe(3);
        manual.ShouldNotBeNull();
    }

    [Fact]
    public void Unmapped_tickers_need_a_decision_before_anything_is_recorded()
    {
        var fixture = new PortfolioFixture();
        var rows = Rows().Select(row => new PricedRow(row, null, row.NeedsInstrument ? null : fixture.A.Value, row.Quantity ?? 0m, row.Price ?? 0m, row.Amount)).ToList();

        var outcomes = StatementMatcher.Match(rows, [], ImportMode.Merge, null);

        outcomes.Count(outcome => outcome.Decision == RowDecision.NeedsMapping).ShouldBe(4);
    }

    private static List<PricedRow> Priced(PortfolioFixture fixture) =>
        [.. Rows().Select(row => new PricedRow(row, row.NeedsInstrument ? PortfolioFixture.Etf : null, fixture.A.Value, row.Quantity ?? 0m, row.Price ?? 0m, row.Amount))];
}
