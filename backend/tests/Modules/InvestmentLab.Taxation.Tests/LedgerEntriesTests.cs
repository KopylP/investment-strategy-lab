using InvestmentLab.Taxation.Ledger.Domain;

namespace InvestmentLab.Taxation.Tests;

public sealed class LedgerEntriesTests
{
    private static readonly Guid Instrument = Guid.NewGuid(), Sub = Guid.NewGuid();

    private sealed record Entry(string Currency, ITaxJournalTrade? Trade = null, ITaxJournalCash? Cash = null) : ITaxJournalEntry
    {
        public Guid TransactionId { get; } = Guid.NewGuid();
        public Guid AccountId { get; } = Guid.NewGuid();
        public UserId OwnerId { get; } = UserId.Of(Guid.NewGuid());
        public DateOnly OccurredOn => new(2025, 4, 22);
    }

    private sealed record Trade(string Side, decimal Quantity, decimal Price, decimal Fee, decimal? SettledAmount = null, string? SettledCurrency = null) : ITaxJournalTrade
    {
        public Guid InstrumentId => Instrument;
        public IReadOnlyList<EntryAllocation> Allocations => [new(Sub, Quantity)];
    }

    private sealed record Cash(string Kind, decimal Amount, Guid? InstrumentId, decimal? SettledAmount = null, string? SettledCurrency = null) : ITaxJournalCash
    {
        public IReadOnlyList<EntryAllocation> Allocations => [new(Sub, Amount)];
    }

    [Fact]
    public void A_usd_trade_on_a_eur_account_is_taxed_in_usd()
    {
        var recorded = new Entry("EUR", new Trade("Buy", 0.15214042m, 457.27m, 0m, SettledAmount: 80m, SettledCurrency: "USD"));

        var entry = LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!;

        entry.Currency.ShouldBe("USD");
        (entry.Quantity * entry.Price).ShouldBe(80m, 0.0001m);
    }

    [Fact]
    public void A_trade_settled_in_the_account_currency_keeps_its_price_and_fee()
    {
        var recorded = new Entry("EUR", new Trade("Sell", 2m, 50m, 3m, SettledAmount: 90m, SettledCurrency: "EUR"));

        var entry = LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!;

        (entry.Kind, entry.Price, entry.Fee, entry.Currency).ShouldBe((TaxableKind.Sell, 50m, 3m, "EUR"));
    }

    [Fact]
    public void A_dividend_settled_in_another_currency_is_taken_in_that_currency()
    {
        var recorded = new Entry("EUR", Cash: new Cash("Dividend", 10m, Instrument, SettledAmount: 11m, SettledCurrency: "USD"));

        var entry = LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!;

        (entry.Kind, entry.Amount, entry.Currency).ShouldBe((TaxableKind.Dividend, 11m, "USD"));
    }

    [Fact]
    public void Cash_that_is_no_dividend_of_an_instrument_is_not_in_the_tax_ledger()
    {
        LedgerEntries.From(new Entry("EUR", Cash: new Cash("Contribution", 10m, null)), DateTimeOffset.UnixEpoch).ShouldBeNull();
        LedgerEntries.From(new Entry("EUR", Cash: new Cash("Dividend", 10m, null)), DateTimeOffset.UnixEpoch).ShouldBeNull();
    }

    [Fact]
    public void A_foreign_sell_converts_price_and_fee_from_what_was_left_after_the_fee()
    {
        var recorded = new Entry("EUR", new Trade("Sell", 2m, 50m, 10m, SettledAmount: 45m, SettledCurrency: "USD"));

        var entry = LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!;

        (entry.Price, entry.Fee, entry.Currency).ShouldBe((25m, 5m, "USD"));
    }

    [Fact]
    public void A_dividend_settled_in_the_account_currency_keeps_its_amount()
    {
        var recorded = new Entry("EUR", Cash: new Cash("Dividend", 10m, Instrument, SettledAmount: 11m, SettledCurrency: "EUR"));

        LedgerEntries.From(recorded, DateTimeOffset.UnixEpoch)!.Amount.ShouldBe(10m);
    }
}
